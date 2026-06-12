using AForge.Video;using AForge.Video.DirectShow;using Microsoft.Win32;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;
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
        private System.Windows.Controls.Image _previewTarget;
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
        private void LoadCameras(System.Windows.Controls.ComboBox target)
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            target.Items.Clear();
            foreach (FilterInfo device in _videoDevices)
            {
                target.Items.Add(device.Name);
            }
            if (target.Items.Count > 0)
                target.SelectedIndex = 0;
            else
                target.Items.Add("Brak podłączonej kamery");
        }
        private void StartCameraOn(System.Windows.Controls.Image target, int deviceIndex)
        {
            if (_videoDevices == null || _videoDevices.Count == 0) return;
            if (deviceIndex < 0 || deviceIndex >= _videoDevices.Count) deviceIndex = 0;
            StopCamera();
            _loadedDiskImagePath = "";
            _previewTarget = target;
            _videoSource = new VideoCaptureDevice(_videoDevices[deviceIndex].MonikerString);
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.Start();
        }
        private void StartCameraBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_videoDevices == null || _videoDevices.Count == 0) return;
            StartCameraOn(WebcamImage, CameraDevicesBox.SelectedIndex);
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
            Dispatcher.BeginInvoke(() =>
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
                        bitmapImage.Freeze();
                        var mirrored = new TransformedBitmap(bitmapImage, new ScaleTransform(-1, 1));
                        mirrored.Freeze();
                        if (_previewTarget != null)
                            _previewTarget.Source = mirrored;
                    }
                }
                catch { }
            });
        }
        private void BulkImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            openFileDialog.Title = "Wybierz zdjęcia profilowe do bazy danych";
            if (openFileDialog.ShowDialog() == true)
            {
                int count = 0;
                foreach (string filename in openFileDialog.FileNames)
                {
                    try
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(filename);
                        FaceEngine.RegisterProfile(name, filename);
                        count++;
                    }
                    catch { }
                }
                MessageBox.Show($"Pomyślnie zaimportowano {count} zdjęć do bazy FaceProfiles.json!", "Hurtowy Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SecurityLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SecurityLevelLabel != null)
                SecurityLevelLabel.Text = $"Poziom bezpieczeństwa (Threshold): {Math.Round(e.NewValue)}%";
        }
        private void ShowLoginPanel_Click(object sender, RoutedEventArgs e)
        {
            PanelMenu.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Collapsed;
            PanelLogin.Visibility = Visibility.Visible;
            _previewTarget = null;
            LoadCameras(CameraDevicesBox);
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
            _previewTarget = null;
            RegisterNameBox.Text = "";
            RegisterFaceImage.Source = null;
            RegisterResultText.Text = "";
            _tempImagePath = "";
            RegisterCaptureBtn.IsEnabled = false;
            RegisterStartCamBtn.Content = "🎥 Włącz Kamerę";
            LoadCameras(RegisterCameraBox);
        }
        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            PanelLogin.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Collapsed;
            PanelMenu.Visibility = Visibility.Visible;
            StopCamera();
        }
        private void EvaluateModel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = FaceEngine.TrainModel(out var metrics);
                if (metrics != null)
                {
                    MessageBox.Show(result + $"\n\nMicro-Accuracy: {metrics.MicroAccuracy:P2}\nMacro-Accuracy: {metrics.MacroAccuracy:P2}\nLog-Loss: {metrics.LogLoss:F4}",

                        "Ewaluacja Modelu ML.NET", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(result, "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas trenowania: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                StopCamera();
                _previewTarget = null;
                RegisterCaptureBtn.IsEnabled = false;
                _tempImagePath = openFileDialog.FileName;
                RegisterFaceImage.Source = LoadImageSafely(_tempImagePath);
                RegisterResultText.Text = "Zdjęcie referencyjne wybrane.";
                RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#AAAAAA");
            }
        }
        private void RegisterStartCamera_Click(object sender, RoutedEventArgs e)
        {
            if (_videoDevices == null || _videoDevices.Count == 0)
            {
                RegisterResultText.Text = "Brak podłączonej kamery.";
                RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                return;
            }
            _tempImagePath = "";
            StartCameraOn(RegisterFaceImage, RegisterCameraBox.SelectedIndex);
            RegisterCaptureBtn.IsEnabled = true;
            RegisterStartCamBtn.Content = "🎥 Kamera działa";
            RegisterResultText.Text = "Ustaw twarz i uchwyć zdjęcie.";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#AAAAAA");
        }
        private void RegisterCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            string capturePath = Path.Combine(Path.GetTempPath(), "neuro_register_capture.jpg");
            lock (_frameLock)
            {
                if (_currentFrame != null)
                {
                    _currentFrame.Save(capturePath, ImageFormat.Jpeg);
                }
                else
                {
                    MessageBox.Show("Nie udało się pobrać klatki z kamery.");
                    return;
                }
            }
            StopCamera();
            _previewTarget = null;
            RegisterCaptureBtn.IsEnabled = false;
            RegisterStartCamBtn.Content = "🎥 Włącz Kamerę";
            _tempImagePath = capturePath;
            var still = LoadImageSafely(capturePath);
            if (still != null)
            {
                still.Freeze();
                var mirroredStill = new TransformedBitmap(still, new ScaleTransform(-1, 1));
                mirroredStill.Freeze();
                RegisterFaceImage.Source = mirroredStill;
            }
            RegisterResultText.Text = "Zdjęcie z kamery uchwycone.";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#AAAAAA");
        }
        private async void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegisterNameBox.Text) || string.IsNullOrEmpty(_tempImagePath))
            {
                MessageBox.Show("Podaj imię i wgraj zdjęcie!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            RegisterSubmitBtn.IsEnabled = false;
            RegisterResultText.Text = "Przetwarzanie profilu biometrycznego...";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#3A86FF");
            string name = RegisterNameBox.Text.Trim();
            string imagePath = _tempImagePath;
            try
            {
                string targetFolder = Path.Combine(GetAppRoot(), "Dane", name.Replace(" ", "_"));
                Directory.CreateDirectory(targetFolder);
                string targetFile = Path.Combine(targetFolder, "ref_" + Path.GetFileName(imagePath));
                File.Copy(imagePath, targetFile, true);
                await Task.Run(() => FaceEngine.RegisterProfile(name, imagePath));
            }
            catch (Exception ex)
            {
                RegisterResultText.Text = "⚠ Błąd zapisu profilu biometrycznego.";
                RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                AppendAuditLog("ERROR", 0f, "Rejestracja nieudana: " + ex.Message);
                RegisterSubmitBtn.IsEnabled = true;
                return;
            }
            _lastRegisteredName = name;
            RegisterResultText.Text = $"✅ Zapisano profil biometryczny: '{name}'";
            RegisterResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#00E676");
            RegisterSubmitBtn.IsEnabled = true;
        }
        private string GetAppRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            while (current != null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(current, "FaceAuthApp.csproj")))
                    return current;
                current = System.IO.Directory.GetParent(current)?.FullName;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        private void AppendAuditLog(string result, float score, string message)
        {
            try
            {
                string logFile = Path.Combine(GetAppRoot(), "Security_Audit.log");
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STATUS: {result} | SCORE: {score:P2} | MSG: {message}{Environment.NewLine}";
                File.AppendAllText(logFile, logLine);
            }
            catch { }
        }
        private void SaveIntruderSnapshot(string imagePath)
        {
            try
            {
                string intrudersDir = Path.Combine(GetAppRoot(), "Zagrożenia");
                Directory.CreateDirectory(intrudersDir);
                string intruderFile = Path.Combine(intrudersDir, $"INTRUZ_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                File.Copy(imagePath, intruderFile, true);
            }
            catch { }
        }
        private async void LoginScan_Click(object sender, RoutedEventArgs e)
        {
            string capturedImagePath = "";
            bool isLivenessPassed = true;
            if (!string.IsNullOrEmpty(_loadedDiskImagePath))
            {
                capturedImagePath = _loadedDiskImagePath;
            }
            else
            {
                capturedImagePath = Path.Combine(Path.GetTempPath(), "neuro_secure_capture.jpg");
                LoginResultText.Text = "Analiza żywotności (Anti-Spoofing)...";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FFAA00");
                int variance = 0;
                Bitmap[] samples = new Bitmap[3];
                for (int i = 0; i < 3; i++)
                {
                    lock (_frameLock) { if (_currentFrame != null) samples[i] = (Bitmap)_currentFrame.Clone(); }
                    await Task.Delay(300);
                }
                if (samples[0] != null && samples[1] != null && samples[2] != null)
                {
                    int width = samples[0].Width;
                    int height = samples[0].Height;
                    int startX = width / 2 - 50;
                    int startY = height / 2 - 50;
                    for (int x = 0; x < 100; x += 5)

                    {
                        for (int y = 0; y < 100; y += 5)
                        {
                            int px = startX + x;
                            int py = startY + y;
                            if (px > 0 && px < width && py > 0 && py < height)
                            {
                                var c1 = samples[0].GetPixel(px, py);
                                var c2 = samples[1].GetPixel(px, py);
                                var c3 = samples[2].GetPixel(px, py);
                                variance += Math.Abs(c1.R - c2.R) + Math.Abs(c2.R - c3.R);
                                variance += Math.Abs(c1.G - c2.G) + Math.Abs(c2.G - c3.G);
                                variance += Math.Abs(c1.B - c2.B) + Math.Abs(c2.B - c3.B);
                            }
                        }
                    }
                    samples[0].Save(capturedImagePath, ImageFormat.Jpeg);
                    samples[0].Dispose(); samples[1].Dispose(); samples[2].Dispose();
                }
                if (variance < 12000) isLivenessPassed = false;
                StopCamera();

            }
            LoginScanBtn.IsEnabled = false;
            if (!isLivenessPassed)
            {
                LoginResultText.Text = "🚨 Zablokowano: Wykryto oszustwo (Statyczny Obraz).";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                AppendAuditLog("DENIED", 0f, "Spoofing zablokowany - brak wariancji żywotności.");
                LoginScanBtn.IsEnabled = true;
                return;
            }
            LoginProgress.Visibility = Visibility.Visible;
            LoginResultText.Text = "Analiza rysów twarzy na serwerze AI...";
            LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#3A86FF");
            float score;
            string predictedLabel;
            try
            {
                var match = await Task.Run(() => FaceEngine.Identify(capturedImagePath));
                predictedLabel = match.Name.Replace("_", " ");
                score = match.Score;
            }
            catch (Exception ex)
            {
                LoginProgress.Visibility = Visibility.Collapsed;
                LoginResultText.Text = "⚠ Błąd analizy biometrycznej.";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                AppendAuditLog("ERROR", 0f, "Wyjatek silnika: " + ex.Message);
                LoginScanBtn.IsEnabled = true;
                return;
            }
            LoginProgress.Visibility = Visibility.Collapsed;
            float currentThreshold = (float)(SecurityLevelSlider.Value / 100.0);
            if (string.IsNullOrEmpty(predictedLabel))
            {
                LoginResultText.Text = "🚨 Brak zarejestrowanych profili w bazie.";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                AppendAuditLog("DENIED", score, "Brak profili - logowanie niemozliwe.");
            }
            else if (score < currentThreshold)
            {
                LoginResultText.Text = "🚨 Odmowa dostępu. Nierozpoznano osoby.";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#FF5555");
                AppendAuditLog("DENIED", score, $"Najblizszy profil '{predictedLabel}' ponizej progu.");
                SaveIntruderSnapshot(capturedImagePath);
            }
            else
            {
                LoginResultText.Text = $"✅ Zalogowano: {predictedLabel} (Podobieństwo: {score:P1})";
                LoginResultText.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#00E676");
                AppendAuditLog("GRANTED", score, $"Pomyślne logowanie użytkownika: {predictedLabel}.");
            }
        }
    }
}
