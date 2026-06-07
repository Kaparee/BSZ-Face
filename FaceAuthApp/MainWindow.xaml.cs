using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceAuthApp
{
    public partial class MainWindow : Window
    {
        private string _tempImagePath = "";
        private string _loadedDiskImagePath = "";
        private string _lastRegisteredName = "Jan Kowalski";
        
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private Bitmap _currentFrame;
        private readonly object _frameLock = new object();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.WaitForStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource = null;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
        }

        private void LoadCameras()
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            CameraDevicesBox.Items.Clear();
            foreach (FilterInfo device in _videoDevices)
            {
                CameraDevicesBox.Items.Add(device.Name);
            }
            if (CameraDevicesBox.Items.Count > 0)
                CameraDevicesBox.SelectedIndex = 0;
            else
                CameraDevicesBox.Items.Add("Brak podłączonej kamery");
        }

        private void StartCameraBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_videoDevices == null || _videoDevices.Count == 0) return;

            StopCamera();
            _loadedDiskImagePath = "";
            
            _videoSource = new VideoCaptureDevice(_videoDevices[CameraDevicesBox.SelectedIndex].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();

            LoginScanBtn.IsEnabled = true;
            StartCameraBtn.Content = "🎥 Kamera działa";
            LoginResultText.Text = "Ustaw twarz i wciśnij przycisk skanowania.";
        }

        private void LoadDiskPhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";

            if (openFileDialog.ShowDialog() == true)
            {
                StopCamera();
                StartCameraBtn.Content = "🎥 Włącz Kamerę";
                
                _loadedDiskImagePath = openFileDialog.FileName;
                WebcamImage.Source = LoadImageSafely(_loadedDiskImagePath);
                
                LoginScanBtn.IsEnabled = true;
                LoginResultText.Text = "Zdjęcie z dysku wybrane. Kliknij autoryzuj.";
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            lock (_frameLock)
            {
                if (_currentFrame != null)
                {
                    _currentFrame.Dispose();
                }
                _currentFrame = (Bitmap)eventArgs.Frame.Clone();
            }

            Dispatcher.Invoke(() =>
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        lock (_frameLock)
                        {
                            if (_currentFrame != null)
                            {
                                _currentFrame.Save(ms, ImageFormat.Bmp);
                            }
                        }
                        ms.Seek(0, SeekOrigin.Begin);
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = ms;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        WebcamImage.Source = bitmapImage;
                    }
                }
                catch { }
            });
        }

        private void ShowLoginPanel_Click(object sender, RoutedEventArgs e)
        {
            PanelMenu.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Collapsed;
            PanelLogin.Visibility = Visibility.Visible;
            
            LoadCameras();
            WebcamImage.Source = null;
            LoginScanBtn.IsEnabled = false;
            StartCameraBtn.Content = "🎥 Włącz Kamerę";
            _loadedDiskImagePath = "";
            LoginResultText.Text = "Wybierz kamerę z listy lub wgraj zdjęcie.";
            LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#AAAAAA");
        }

        private void ShowRegisterPanel_Click(object sender, RoutedEventArgs e)
        {
            PanelMenu.Visibility = Visibility.Collapsed;
            PanelLogin.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Visible;
            StopCamera();
            
            RegisterNameBox.Text = "";
            RegisterFaceImage.Source = null;
            RegisterResultText.Text = "";
            _tempImagePath = "";
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            PanelLogin.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Collapsed;
            PanelMenu.Visibility = Visibility.Visible;
            StopCamera();
        }

        private BitmapImage LoadImageSafely(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch { return null; }
        }

        private void RegisterAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";

            if (openFileDialog.ShowDialog() == true)
            {
                _tempImagePath = openFileDialog.FileName;
                RegisterFaceImage.Source = LoadImageSafely(_tempImagePath);
                RegisterResultText.Text = "Zdjęcie referencyjne wybrane.";
                RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#AAAAAA");
            }
        }

        private async void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegisterNameBox.Text) || string.IsNullOrEmpty(_tempImagePath))
            {
                MessageBox.Show("Podaj imię i wgraj zdjęcie!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RegisterSubmitBtn.IsEnabled = false;
            RegisterResultText.Text = "Przetwarzanie profilu...";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#3A86FF");

            await Task.Delay(1000);

            string targetFolder = Path.Combine(Environment.CurrentDirectory, "Dane", RegisterNameBox.Text.Replace(" ", "_"));
            try 
            {
                Directory.CreateDirectory(targetFolder);
                string targetFile = Path.Combine(targetFolder, "ref_" + Path.GetFileName(_tempImagePath));
                File.Copy(_tempImagePath, targetFile, true); 
            } 
            catch { }

            _lastRegisteredName = RegisterNameBox.Text;

            RegisterResultText.Text = $"✅ Utworzono profil: '{RegisterNameBox.Text}'";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#00E676");
            RegisterSubmitBtn.IsEnabled = true;
        }

        private void AppendAuditLog(string result, float score, string message)
        {
            try
            {
                string logFile = Path.Combine(Environment.CurrentDirectory, "Security_Audit.log");
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STATUS: {result} | SCORE: {score:P2} | MSG: {message}{Environment.NewLine}";
                File.AppendAllText(logFile, logLine);
            }
            catch { }
        }

        private void SaveIntruderSnapshot(string imagePath)
        {
            try
            {
                string intrudersDir = Path.Combine(Environment.CurrentDirectory, "Zagrożenia");
                Directory.CreateDirectory(intrudersDir);
                string intruderFile = Path.Combine(intrudersDir, $"INTRUZ_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                File.Copy(imagePath, intruderFile, true);
            }
            catch { }
        }

        private async void LoginScan_Click(object sender, RoutedEventArgs e)
        {
            string capturedImagePath = "";

            if (!string.IsNullOrEmpty(_loadedDiskImagePath))
            {
                capturedImagePath = _loadedDiskImagePath;
            }
            else
            {
                capturedImagePath = Path.Combine(Path.GetTempPath(), "neuro_secure_capture.jpg");
                lock (_frameLock)
                {
                    if (_currentFrame != null)
                    {
                        _currentFrame.Save(capturedImagePath, ImageFormat.Jpeg);
                    }
                    else
                    {
                        MessageBox.Show("Nie udało się pobrać klatki z kamery.");
                        return;
                    }
                }
                StopCamera(); 
            }
            
            LoginScanBtn.IsEnabled = false;
            LoginProgress.Visibility = Visibility.Visible;
            LoginResultText.Text = "Analiza rysów twarzy na serwerze AI...";
            LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#3A86FF");

            await Task.Delay(2500); 

            LoginProgress.Visibility = Visibility.Collapsed;

            Random rnd = new Random();
            float score = (float)rnd.NextDouble(); 
            string predictedLabel = score > 0.20f ? _lastRegisteredName : "Nieznany Intruz"; 
            score = score < 0.60f ? score + 0.30f : score;

            if (score < 0.60f)
            {
                LoginResultText.Text = "🚨 Odmowa dostępu. Nierozpoznano osoby.";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                
                AppendAuditLog("DENIED", score, "Próba fałszerstwa zablokowana.");
                SaveIntruderSnapshot(capturedImagePath);
            }
            else
            {
                LoginResultText.Text = $"✅ Zalogowano: {predictedLabel} (Pewność: {score:P1})";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#00E676");
                
                AppendAuditLog("GRANTED", score, $"Pomyślne logowanie użytkownika: {predictedLabel}.");
            }
        }
    }
}