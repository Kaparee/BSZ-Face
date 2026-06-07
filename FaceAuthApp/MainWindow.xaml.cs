using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FaceAuthApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";

            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                
                // Wyświetl obraz
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                FaceImage.Source = bitmap;

                // TODO: Tutaj wkleisz integrację z modelem wygenerowanym przez Radzia
                PredictAndDisplayResult(imagePath);
            }
        }

        private void PredictAndDisplayResult(string imagePath)
        {
            // MOCK: Symulacja zwrócenia wyniku z modelu
            // W docelowej wersji użyjesz wygenerowanej klasy ConsumeModel.cs. Przykład:
            //
            // var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            // var input = new ConsumeModel.ModelInput() { ImageSource = imageBytes };
            // var result = ConsumeModel.Predict(input);
            // string predictedLabel = result.PredictedLabel;
            // float score = result.Score.Max();

            // Symulowane dane (do usunięcia po wpięciu ML.NET)
            Random rnd = new Random();
            float score = (float)rnd.NextDouble(); // Losowy score od 0.0 do 1.0
            string predictedLabel = score > 0.5f ? "Elon_Musk" : "Bill_Gates"; 

            // Logika bezpieczeństwa - Thresholding (Próg akceptacji: 60%)
            if (score < 0.60f)
            {
                ResultLabel.Text = "Osoba nierozpoznana / Możliwe fałszerstwo";
                ResultLabel.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                ResultLabel.Text = $"Rozpoznano: {predictedLabel} (Pewność: {score:P1})";
                ResultLabel.Foreground = System.Windows.Media.Brushes.Green;
            }
        }
    }
}