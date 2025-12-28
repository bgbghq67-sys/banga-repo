using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cv = OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Threading;

namespace Banga_Photobooth
{
    public partial class CaptureWindow : Window
    {
        // Simulation mode inherited from TemplateWindow
        private bool simulationMode;
        // Use temp folder for debug logs to avoid permission issues in Program Files
        private static readonly string DEBUG_LOG_FILE = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "banga_camera_debug.log");

        // Countdown
        private readonly DispatcherTimer countdownTimer;
        private int currentCount = 5;
        private readonly Storyboard rightCountdownStepAnimation;
        private readonly Storyboard rightSmileAnimation;

        // Camera preview (simulation)
        private readonly DispatcherTimer previewTimer;
        private readonly System.Collections.Generic.List<BitmapImage> demoImages = new();
        private int demoIndex = 0;
        private ImageSource? lastPreviewFrame;

        // Webcam (simulation live)
        private Cv.VideoCapture? webcam;
        private DispatcherTimer? webcamTimer;

        // Captured preview rotation
        private int capturedCount = 0;

        // Selected template and metadata to pass forward
        private readonly BitmapImage? selectedTemplate;
        private readonly TemplateMetadata? selectedTemplateMetadata;

        public CaptureWindow(BitmapImage? template = null, TemplateMetadata? metadata = null)
        {
            InitializeComponent();
            selectedTemplate = template;
            selectedTemplateMetadata = metadata;
            
            // Adjust camera preview dimensions based on template photo slot aspect ratio
            if (selectedTemplateMetadata?.PhotoSlots?.Count > 0)
            {
                var slot = selectedTemplateMetadata.PhotoSlots[0];
                // If slot is Portrait (Width < Height), adjust preview to Portrait
                if (slot.Width < slot.Height)
                {
                    // Target: 3:4 aspect ratio approx
                    // Reduced size to avoid being too dominant and overlap with countdown
                    // 600x800 is a good balance
                    CenterPreviewBorder.Width = 600;
                    CenterPreviewBorder.Height = 800;
                }
                else
                {
                    // Landscape slot - keep default 998x702
                    CenterPreviewBorder.Width = 998;
                    CenterPreviewBorder.Height = 702;
                }
            }
            
            // Get simulation mode from Config
            simulationMode = BangaConfig.Current.CameraSimulationMode;

            // Initialize debug log
            LogDebug("=== CAPTURE WINDOW INITIALIZED ===");
            LogDebug($"Camera Simulation Mode: {simulationMode} (True=Webcam, False=Canon)");
            LogDebug($"Selected Webcam Index: {BangaConfig.Current.SelectedWebcamIndex}");

            // Setup countdown
            countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            countdownTimer.Tick += CountdownTimer_Tick;
            rightCountdownStepAnimation = (Storyboard)FindResource("RightCountdownStepAnimation");
            rightSmileAnimation = (Storyboard)FindResource("RightSmileAnimation");

            // Setup simulated camera preview if enabled
            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            previewTimer.Tick += PreviewTimer_Tick;

            Loaded += CaptureWindow_Loaded;
        }
        
        private void LogDebug(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            System.Diagnostics.Debug.WriteLine(logMessage);
            try
            {
                File.AppendAllText(DEBUG_LOG_FILE, logMessage + "\n");
            }
            catch { /* Ignore file write errors */ }
        }
        

        // Wait for camera ready logic
        private DispatcherTimer? cameraWaitTimer;
        private DateTime _cameraInitStartTime;
        private const double MIN_WAIT_SECONDS = 4.0;
        private const int MAX_CAMERA_WAIT_SECONDS = 15; // Increased timeout to accommodate fixed delay

        private async void CaptureWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Preload sound
            PreloadSmileSound();

            // Load background image from file path
            var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            var bgPath = System.IO.Path.Combine(assetsPath, "mainbg.png");
            if (System.IO.File.Exists(bgPath))
            {
                var mainGrid = ((Viewbox)Content).Child as Grid;
                if (mainGrid != null)
                {
                    var bgBrush = new ImageBrush(new BitmapImage(new Uri(bgPath)));
                    mainGrid.Background = bgBrush;
                }
            }
            
            // Instead of starting countdown immediately, we wait for the first frame AND a minimum delay
            LoadingPanel.Visibility = Visibility.Visible;
            _cameraInitStartTime = DateTime.Now;
            
            cameraWaitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            cameraWaitTimer.Tick += CameraWaitTimer_Tick;
            cameraWaitTimer.Start();

            // Offload camera initialization to background thread to keep UI responsive
            await InitializeCameraAsync();
        }

        private async System.Threading.Tasks.Task InitializeCameraAsync()
        {
            LogDebug($"=== InitializeCameraAsync called, Simulation Mode: {simulationMode} ===");
            
            // Initialize the timer on UI thread but don't start it yet
            webcamTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(66) }; // ~15 fps
            webcamTimer.Tick += WebcamTimer_Tick;
            
            if (simulationMode)
            {
                // Simulation is usually fast, but good practice to keep consistent
                LogDebug("SIMULATION MODE: Attempting to start webcam...");
                int camIndex = BangaConfig.Current.SelectedWebcamIndex;
                
                await System.Threading.Tasks.Task.Run(() => 
                {
                    AttemptConnectWebcam(camIndex);
                });
            }
            else
            {
                // Canon connection is slow (probing devices), so we run it in background
                LogDebug("CANON MODE: Attempting to connect to Canon EOS 2000D...");
                
                await System.Threading.Tasks.Task.Run(() => 
                {
                    AttemptConnectCanon();
                });
            }

            // Post-initialization checks (Back on UI Thread)
                if (webcam is null || !webcam.IsOpened())
            {
                if (simulationMode)
                {
                    LogDebug("Webcam failed, loading demo images...");
                    LoadDemoImages();
                    if (demoImages.Count > 0)
                    {
                        SetRectangleImage(CameraFeedRect, demoImages[0]);
                        lastPreviewFrame = demoImages[0];
                        demoIndex = 1 % demoImages.Count;
                        previewTimer.Start();
                        LogDebug($"Demo images loaded: {demoImages.Count} images");
                    }
                    else
                    {
                        LogDebug("ERROR: No demo images found!");
                }
            }
            else
                {
                    LogDebug("ERROR: Canon camera not detected!");
                    MessageBox.Show(
                        "Canon EOS 2000D not detected!\n\n" +
                        "Falling back to Simulation Mode...",
                        "Canon Camera Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    
                    // Fall back to simulation mode
                    simulationMode = true;
                    TemplateWindow.SimulationMode = true;
                    // Retry initialization in simulation mode
                    await InitializeCameraAsync(); 
                }
            }
            else
            {
                // Camera opened successfully
                webcamTimer.Start();
            }
        }

        private void AttemptConnectWebcam(int camIndex)
        {
            LogDebug($"Trying webcam index {camIndex}...");
            if (TryOpenBackend(Cv.VideoCaptureAPIs.MSMF, camIndex)) return;
            if (TryOpenBackend(Cv.VideoCaptureAPIs.DSHOW, camIndex)) return;
            if (TryOpenBackend(Cv.VideoCaptureAPIs.ANY, camIndex)) return;
            
            LogDebug("Selected webcam index failed. Falling back to default 0...");
            if (TryOpenBackend(Cv.VideoCaptureAPIs.ANY, 0)) return;
        }

        private void AttemptConnectCanon()
        {
            LogDebug("=== AttemptConnectCanon: Starting Canon EOS 2000D detection ===");
            try
            {
                LogDebug("Scanning for Canon camera on different device indices...");
                for (int deviceIndex = 0; deviceIndex < 5; deviceIndex++)
                {
                    LogDebug($"Trying device index {deviceIndex}...");
                    if (TryOpenBackend(Cv.VideoCaptureAPIs.DSHOW, deviceIndex))
                    {
                         if (CheckAndConfigureCanon(deviceIndex, "DSHOW")) return;
                    }
                    else if (TryOpenBackend(Cv.VideoCaptureAPIs.MSMF, deviceIndex))
                    {
                         if (CheckAndConfigureCanon(deviceIndex, "MSMF")) return;
                    }
                }
                LogDebug("ERROR: No Canon camera found on any device index!");
            }
            catch (Exception ex)
            {
                LogDebug($"EXCEPTION in AttemptConnectCanon: {ex.Message}");
                webcam = null;
            }
        }

        private bool CheckAndConfigureCanon(int deviceIndex, string apiName)
        {
            try 
            {
                // Check if we can read a frame
                using var testMat = new Cv.Mat();
                if (webcam!.Read(testMat) && !testMat.Empty())
                {
                    LogDebug($"SUCCESS: Device {deviceIndex} ({apiName}) works! Size: {testMat.Width}x{testMat.Height}");
                    
                    // Set higher resolution
                    webcam.Set(Cv.VideoCaptureProperties.FrameWidth, 1920);
                    webcam.Set(Cv.VideoCaptureProperties.FrameHeight, 1080);
                    
                    // Enable Autofocus (CAP_PROP_AUTOFOCUS = 39 in OpenCV)
                    webcam.Set((Cv.VideoCaptureProperties)39, 1);
                    LogDebug("Autofocus ENABLED");
                    
                    var actualWidth = webcam.Get(Cv.VideoCaptureProperties.FrameWidth);
                    var actualHeight = webcam.Get(Cv.VideoCaptureProperties.FrameHeight);
                    LogDebug($"Canon camera resolution set to: {actualWidth}x{actualHeight}");
                    return true;
                }
                else
                {
                    LogDebug($"Device {deviceIndex} ({apiName}) cannot capture frames");
                    webcam.Release();
                    webcam = null;
                }
            }
            catch (Exception ex)
            {
                 LogDebug($"Error configuring Canon: {ex.Message}");
                 webcam?.Release();
                 webcam = null;
            }
            return false;
        }

        // Existing TryStartPreview and TryStartCanonCamera removed/replaced by above logic


        private void CameraWaitTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = (DateTime.Now - _cameraInitStartTime).TotalSeconds;
            bool isTimeUp = elapsed >= MIN_WAIT_SECONDS;
            bool isCameraReady = lastPreviewFrame != null || (simulationMode && demoImages.Count > 0);

            // We wait until BOTH the minimum time has passed AND the camera is ready
            if (isTimeUp && isCameraReady)
            {
                // Camera is ready and min wait time passed!
                cameraWaitTimer?.Stop();
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                LogDebug($"Camera ready and wait time ({elapsed:F1}s) passed. Starting countdown...");
                StartCountdownSequence();
            }
            else if (elapsed > MAX_CAMERA_WAIT_SECONDS)
            {
                // Timeout waiting for camera (even if min wait passed)
                cameraWaitTimer?.Stop();
                LoadingPanel.Visibility = Visibility.Collapsed;
                LogDebug("Timeout waiting for camera frame. Starting countdown anyway...");
                MessageBox.Show("Camera initialization timed out. Please checks connections.", "Camera Warning");
                StartCountdownSequence();
            }
        }

        private void StartCountdownSequence()
        {
            currentCount = BangaConfig.Current.CountdownSeconds;
            ShowCountdown(currentCount.ToString(), Color.FromRgb(0x00, 0xA6, 0xFF));
            countdownTimer.Start();
            PlayCountdownStepAnimation();
        }

        
        private void StopCamera()
        {
            LogDebug("=== Stopping camera ===");
            webcamTimer?.Stop();
            previewTimer?.Stop();
            try 
            { 
                webcam?.Release(); 
                LogDebug("Camera released successfully");
            } 
            catch (Exception ex) 
            { 
                LogDebug($"Error releasing camera: {ex.Message}");
            }
            webcam = null;
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            if (!simulationMode || demoImages.Count == 0) return;
            SetRectangleImage(CameraFeedRect, demoImages[demoIndex]);
            lastPreviewFrame = demoImages[demoIndex];
            demoIndex = (demoIndex + 1) % demoImages.Count;
        }

        private void LoadDemoImages()
        {
            try
            {
                // Try typical Debug layout: bin/Debug/netX → ../../../Demo
                var relativeDemo = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Demo"));
                string? demoDir = null;
                if (Directory.Exists(relativeDemo))
                {
                    demoDir = relativeDemo;
                }
                else
                {
                    // Fallback: walk up to project root and append Demo
                    var netDir = Directory.GetParent(AppContext.BaseDirectory);
                    var debugDir = netDir?.Parent;
                    var binDir = debugDir?.Parent;
                    var projectRoot = binDir?.Parent?.FullName ?? binDir?.FullName;
                    var fallback = projectRoot is null ? null : System.IO.Path.Combine(projectRoot, "Demo");
                    if (fallback is not null && Directory.Exists(fallback))
                    {
                        demoDir = fallback;
                    }
                }

                if (demoDir is null) return;

                var files = Directory.EnumerateFiles(demoDir)
                    .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f);

                foreach (var file in files)
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.UriSource = new Uri(file);
                    bi.EndInit();
                    bi.Freeze();
                    demoImages.Add(bi);
                }
            }
            catch { /* swallow for dev convenience */ }
        }


        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            currentCount--;
            if (currentCount > 0)
            {
                ShowCountdown(currentCount.ToString(), Color.FromRgb(0x00, 0xA6, 0xFF));
                PlayCountdownStepAnimation();
                
                // Play sound 0.5s before "Smile!" (which appears when currentCount == 0)
                if (currentCount == 1)
                {
                    Dispatcher.BeginInvoke(async () => 
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        PlaySmileSound();
                    });
                }
            }
            else if (currentCount == 0)
            {
                ShowCountdown("Smile!", Color.FromRgb(0x00, 0xA6, 0xFF));
                PlaySmileAnimation();
            }
            else
            {
                countdownTimer.Stop();
                HideCountdown();
                CapturePhoto();
                
                // Auto-restart countdown for next photo after a brief pause
                Dispatcher.BeginInvoke(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(800); // 0.8s pause
                    if (capturedCount < 6)
                    {
                        currentCount = BangaConfig.Current.CountdownSeconds;
                        ShowCountdown(currentCount.ToString(), Color.FromRgb(0x00, 0xA6, 0xFF));
                        countdownTimer.Start();
                        PlayCountdownStepAnimation();
                    }
                }, DispatcherPriority.Background);
            }
        }

        private MediaPlayer _smileSoundPlayer = new MediaPlayer();
        private bool _isSoundLoaded = false;

        private void PreloadSmileSound()
        {
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Smile.mp3");
                if (System.IO.File.Exists(path))
                {
                    _smileSoundPlayer.Open(new Uri(path));
                    _isSoundLoaded = true;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error preloading sound: {ex.Message}");
            }
        }

        private void PlaySmileSound()
        {
            try
            {
                if (_isSoundLoaded)
                {
                    _smileSoundPlayer.Stop();
                    _smileSoundPlayer.Position = TimeSpan.Zero;
                    _smileSoundPlayer.Play();
                }
                else
                {
                    // Fallback if not preloaded
                     var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Smile.mp3");
                     if (System.IO.File.Exists(path))
                     {
                         _smileSoundPlayer.Open(new Uri(path));
                         _smileSoundPlayer.Play();
                         _isSoundLoaded = true;
                     }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error playing sound: {ex.Message}");
            }
        }

        private void ShowCountdown(string text, Color color)
        {
            RightCountdownTextMain.Text = text;
            CountdownOutline1.Text = text;
            CountdownOutline2.Text = text;
            CountdownOutline3.Text = text;
            CountdownOutline4.Text = text;
            RightCountdownHost.Opacity = 1;
            RightCountdownTextMain.Foreground = new SolidColorBrush(color);
        }

        private void HideCountdown()
        {
            RightCountdownHost.Opacity = 0;
        }

        private void PlayCountdownStepAnimation()
        {
            rightCountdownStepAnimation.Begin();
        }

        private void PlaySmileAnimation()
        {
            rightSmileAnimation.Begin();
        }

        private void CapturePhoto()
        {
            LogDebug($"=== CapturePhoto called, capturedCount={capturedCount}, simulationMode={simulationMode} ===");
            
            // Prefer the last preview frame regardless of mode
            ImageSource? image = lastPreviewFrame;
            LogDebug($"lastPreviewFrame is {(image != null ? "available" : "null")}");

            // In simulation with demo images, fall back to the last demo if needed
            if (image is null && simulationMode && demoImages.Count > 0)
            {
                int lastIndex = (demoIndex - 1 + demoImages.Count) % demoImages.Count;
                image = demoImages[lastIndex];
                LogDebug($"Using demo image at index {lastIndex}");
            }

            // In non-simulation (Canon mode), attempt an immediate capture if available
            if (image is null && !simulationMode && webcam is not null && webcam.IsOpened())
            {
                LogDebug("Attempting immediate Canon camera capture...");
                try
                {
                    using var mat = new Cv.Mat();
                    if (webcam.Read(mat) && !mat.Empty())
                    {
                        LogDebug($"Canon capture successful! Frame size: {mat.Width}x{mat.Height}");
                        
                        // Apply horizontal flip if InvertCamera is enabled
                        if (BangaConfig.Current.InvertCamera)
                        {
                            Cv.Cv2.Flip(mat, mat, Cv.FlipMode.Y);
                        }
                        
                        var bs = mat.ToBitmapSource();
                        bs.Freeze();
                        image = bs;
                        lastPreviewFrame = bs;
                    }
                    else
                    {
                        LogDebug("ERROR: Canon camera failed to read frame!");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"EXCEPTION during Canon capture: {ex.Message}");
                }
            }

            if (image is not null)
            {
                // Fill left column first (1→2→3), then right (1→2→3), then cycle
                var slots = new[] { LeftPreview1, LeftPreview2, LeftPreview3, RightPreview1, RightPreview2, RightPreview3 };
                var target = slots[capturedCount % slots.Length];
                SetBorderImage(target, image);
                capturedCount++;
                // After six photos, auto-advance to choosing page with a dramatic pause
                if (capturedCount == 6)
                {
                    LogDebug("All 6 photos captured! Preparing to advance to PreviewWindow...");
                    Dispatcher.BeginInvoke(async () =>
                    {
                        try
                    {
                        await System.Threading.Tasks.Task.Delay(1200); // 1.2 s dramatic pause
                        var images = new System.Collections.Generic.List<ImageSource>();
                        foreach (var b in new[] { LeftPreview1, LeftPreview2, LeftPreview3, RightPreview1, RightPreview2, RightPreview3 })
                        {
                            if (b.Background is ImageBrush ib && ib.ImageSource is ImageSource src)
                                images.Add(src);
                        }
                        if (images.Count == 0)
                        {
                            LogDebug("ERROR: No images collected from preview slots!");
                            return;
                        }
                        
                        LogDebug("=== CaptureWindow: Creating PreviewWindow ===");
                        LogDebug($"images count: {images.Count}");
                        LogDebug($"selectedTemplate: {selectedTemplate != null}");
                        LogDebug($"selectedTemplateMetadata: {selectedTemplateMetadata != null}");
                        if (selectedTemplate != null)
                        {
                            LogDebug($"selectedTemplate URI: {selectedTemplate.UriSource}");
                        }
                        
                        var preview = new PreviewWindow(images, selectedTemplate, selectedTemplateMetadata);
                            LogDebug("PreviewWindow created successfully, showing...");
                        preview.Show();
                            LogDebug("PreviewWindow shown, closing CaptureWindow...");
                        this.Close();
                        }
                        catch (Exception ex)
                        {
                            LogDebug($"!!! CRITICAL ERROR navigating to PreviewWindow: {ex.Message}");
                            LogDebug($"Stack: {ex.StackTrace}");
                            MessageBox.Show($"Error: {ex.Message}\n\nCheck camera_debug.log for details.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }, DispatcherPriority.Render);
                }
                else
                {
                    LogDebug($"Photo {capturedCount}/6 captured successfully");
                }
            }
            else
            {
                LogDebug("ERROR: No image available to capture!");
                MessageBox.Show("No preview frame available to capture. Ensure the camera is working correctly.");
            }
        }

        private static void SetBorderImage(Border border, ImageSource image)
        {
            border.Background = new ImageBrush(image)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }

        private static void SetRectangleImage(Rectangle rect, ImageSource image)
        {
            var brush = new ImageBrush(image)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            // Slightly shrink to keep sampling inside the image at corners,
            // preventing outside-of-texture samples that manifest as dark vertices.
            brush.RelativeTransform = new ScaleTransform(0.995, 0.995, 0.5, 0.5);

            // High-quality scaling to reduce aliasing artifacts.
            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);

            rect.Fill = brush;
        }



        private bool TryOpenBackend(Cv.VideoCaptureAPIs api, int deviceIndex = 0)
        {
            try
            {
                LogDebug($"TryOpenBackend: API={api}, DeviceIndex={deviceIndex}");
                var cap = new Cv.VideoCapture(deviceIndex, api);
                if (cap.IsOpened())
                {
                    LogDebug($"SUCCESS: Camera opened! API={api}, DeviceIndex={deviceIndex}");
                    webcam = cap;
                    return true;
                }
                LogDebug($"FAILED: Camera not opened. API={api}, DeviceIndex={deviceIndex}");
                cap.Release();
            }
            catch (Exception ex)
            {
                LogDebug($"EXCEPTION in TryOpenBackend: {ex.Message}");
            }
            return false;
        }

        private void WebcamTimer_Tick(object? sender, EventArgs e)
        {
            if (webcam is null || !webcam.IsOpened())
            {
                LogDebug("WebcamTimer_Tick: webcam is null or not opened");
                return;
            }
            
            using var mat = new Cv.Mat();
            if (!webcam.Read(mat) || mat.Empty())
            {
                LogDebug("WebcamTimer_Tick: Failed to read frame or frame is empty");
                return;
            }

            // Apply horizontal flip if InvertCamera is enabled
            if (BangaConfig.Current.InvertCamera)
            {
                Cv.Cv2.Flip(mat, mat, Cv.FlipMode.Y); // Y = horizontal flip
            }

            var bs = mat.ToBitmapSource();
            bs.Freeze();
            lastPreviewFrame = bs;
            SetRectangleImage(CameraFeedRect, bs);
        }

        protected override void OnClosed(EventArgs e)
        {
            LogDebug("=== CaptureWindow closing, cleaning up resources ===");
            base.OnClosed(e);
            webcamTimer?.Stop();
            previewTimer?.Stop();
            try 
            { 
                webcam?.Release();
                LogDebug("Camera released on window close");
            } 
            catch (Exception ex)
            {
                LogDebug($"Error releasing camera on close: {ex.Message}");
            }
            webcam = null;
            LogDebug("=== CaptureWindow closed ===");
        }
    }
}