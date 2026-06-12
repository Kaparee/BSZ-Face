using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FaceAuthApp
{
    public class FaceProfile
    {
        public string Name { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    public class FaceData
    {
        [VectorType(128)]
        [ColumnName("Features")]
        public float[] Features { get; set; }
        [ColumnName("Label")]
        public string Label { get; set; }
    }

    public class FacePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; }
        [ColumnName("Score")]
        public float[] Score { get; set; }
    }

    public static class FaceEngine
    {
        public const float MatchThreshold = 0.65f;

        private static readonly string ProfilesPath =
            Path.Combine(AppContext.BaseDirectory, "FaceProfiles.json");

        private static readonly string ModelPath =
            Path.Combine(AppContext.BaseDirectory, "face_sface.onnx");

        private class ImageInput
        {
            public string ImagePath { get; set; } = "";
        }

        private class ImageEmbedding
        {
            [ColumnName("fc1")]
            public float[] Features { get; set; } = Array.Empty<float>();
        }

        private static readonly Lazy<PredictionEngine<ImageInput, ImageEmbedding>> _engine =
            new Lazy<PredictionEngine<ImageInput, ImageEmbedding>>(CreateEngine, true);

        private static PredictionEngine<ImageInput, ImageEmbedding> CreateEngine()
        {
            var ml = new MLContext();

            var pipeline = ml.Transforms.LoadImages("ImageObject", "", nameof(ImageInput.ImagePath))
                .Append(ml.Transforms.ResizeImages("ImageObject", imageWidth: 112, imageHeight: 112,
                    inputColumnName: "ImageObject",
                    resizing: Microsoft.ML.Transforms.Image.ImageResizingEstimator.ResizingKind.Fill))
                .Append(ml.Transforms.ExtractPixels(outputColumnName: "data", inputColumnName: "ImageObject",
                    interleavePixelColors: false, offsetImage: 0f, scaleImage: 1f))
                .Append(ml.Transforms.ApplyOnnxModel(
                    outputColumnNames: new[] { "fc1" },
                    inputColumnNames: new[] { "data" },
                    modelFile: ModelPath));

            var emptyData = ml.Data.LoadFromEnumerable(new List<ImageInput>());
            var model = pipeline.Fit(emptyData);
            return ml.Model.CreatePredictionEngine<ImageInput, ImageEmbedding>(model);
        }

        public static float[] ExtractEmbedding(string imagePath)
        {
            var result = _engine.Value.Predict(new ImageInput { ImagePath = imagePath });
            return Normalize(result.Features);
        }

        private static float[] Normalize(float[] v)
        {
            double norm = Math.Sqrt(v.Sum(x => (double)x * x));
            if (norm < 1e-8) return v;
            return v.Select(x => (float)(x / norm)).ToArray();
        }

        public static float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0) return 0f;
            double dot = 0;
            for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];
            return (float)dot;
        }

        public static List<FaceProfile> LoadProfiles()
        {
            try
            {
                if (!File.Exists(ProfilesPath)) return new List<FaceProfile>();
                string json = File.ReadAllText(ProfilesPath);
                return JsonSerializer.Deserialize<List<FaceProfile>>(json) ?? new List<FaceProfile>();
            }
            catch
            {
                return new List<FaceProfile>();
            }
        }

        public static void SaveProfiles(List<FaceProfile> profiles)
        {
            string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilesPath, json);
        }

        public static void RegisterProfile(string name, string imagePath)
        {
            var profiles = LoadProfiles();
            var embedding = ExtractEmbedding(imagePath);
            profiles.Add(new FaceProfile { Name = name, Embedding = embedding });
            SaveProfiles(profiles);
        }

        public static (string Name, float Score) Identify(string imagePath)
        {
            var profiles = LoadProfiles();
            if (profiles.Count == 0) return ("", 0f);

            var probe = ExtractEmbedding(imagePath);

            // Jeśli mamy co najmniej 2 RÓŻNE osoby, używamy prawdziwego klasyfikatora ML.NET
            var distinctLabels = profiles.Select(p => p.Name).Distinct().Count();
            if (distinctLabels >= 2)
            {
                if (_trainedModel == null) TrainModel(out _);
                if (_predictionEngineML != null)
                {
                    var pred = _predictionEngineML.Predict(new FaceData { Features = probe });
                    float score = pred.Score.Length > 0 ? pred.Score.Max() : 0f;
                    // Wynik SdcaMaximumEntropy to prawdopodobieństwo (0-1)
                    return (pred.PredictedLabel, score);
                }
            }

            // Fallback (np. tylko 1 osoba w bazie) -> Cosine Similarity
            string bestName = "";
            float bestScore = -1f;
            foreach (var p in profiles)
            {
                float s = CosineSimilarity(probe, p.Embedding);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestName = p.Name;
                }
            }
            return (bestName, bestScore);
        }

        private static ITransformer _trainedModel;
        private static PredictionEngine<FaceData, FacePrediction> _predictionEngineML;

        public static string TrainModel(out MulticlassClassificationMetrics metrics)
        {
            metrics = null;
            var ml = new MLContext(seed: 0);
            var profiles = LoadProfiles();

            // === KROK 1: Deduplikacja - usuń profile z identycznym embeddingiem pod inną nazwą ===
            var clean = new List<FaceProfile>();
            foreach (var p in profiles)
            {
                bool isDuplicate = false;
                foreach (var c in clean)
                {
                    if (c.Name == p.Name) continue; // ten sam user, ok
                    float sim = CosineSimilarity(p.Embedding, c.Embedding);
                    if (sim > 0.92f) { isDuplicate = true; break; }
                }
                if (!isDuplicate) clean.Add(p);
            }
            profiles = clean;

            var distinctLabels = profiles.Select(p => p.Name).Distinct().Count();
            if (distinctLabels < 2) 
                return "Wymagane są minimum 2 różne profile do treningu modelu ML.";

            // === KROK 2: Augmentacja danych do treningu (seed A) ===
            var trainData = new List<FaceData>();
            var rndTrain = new Random(42);
            foreach (var p in profiles)
            {
                trainData.Add(new FaceData { Features = p.Embedding, Label = p.Name });
                for (int i = 0; i < 19; i++)
                {
                    var noisy = p.Embedding.ToArray();
                    for (int j = 0; j < noisy.Length; j++) noisy[j] += (float)(rndTrain.NextDouble() * 0.01 - 0.005);
                    trainData.Add(new FaceData { Features = Normalize(noisy), Label = p.Name });
                }
            }

            // === KROK 3: Osobny zbiór ewaluacyjny (seed B - inne szumy) ===
            var testData = new List<FaceData>();
            var rndTest = new Random(999);
            foreach (var p in profiles)
            {
                for (int i = 0; i < 5; i++)
                {
                    var noisy = p.Embedding.ToArray();
                    for (int j = 0; j < noisy.Length; j++) noisy[j] += (float)(rndTest.NextDouble() * 0.01 - 0.005);
                    testData.Add(new FaceData { Features = Normalize(noisy), Label = p.Name });
                }
            }

            // === KROK 4: Trening na CAŁYM zbiorze treningowym ===
            var pipeline = ml.Transforms.Conversion.MapValueToKey("Label")
                .Append(ml.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                    labelColumnName: "Label", featureColumnName: "Features",
                    l1Regularization: 0f, l2Regularization: 0f))
                .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var trainView = ml.Data.LoadFromEnumerable(trainData);
            _trainedModel = pipeline.Fit(trainView);
            _predictionEngineML = ml.Model.CreatePredictionEngine<FaceData, FacePrediction>(_trainedModel);

            // === KROK 5: Ewaluacja na osobnym zbiorze testowym ===
            var testView = ml.Data.LoadFromEnumerable(testData);
            var predictions = _trainedModel.Transform(testView);
            metrics = ml.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");

            return $"Trenowanie zakończone.\nSkuteczność (Accuracy): {metrics.MacroAccuracy:P2}\nLog-Loss: {metrics.LogLoss:F4}";
        }
    }
}

