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
    }
}
