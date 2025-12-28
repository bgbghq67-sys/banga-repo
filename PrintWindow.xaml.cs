using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Banga_Photobooth.Services;
using QRCoder;
using System.Runtime.InteropServices;
using System.Printing;
using System.Linq; // For LINQ queries

namespace Banga_Photobooth
{
    public partial class PrintWindow : Window
    {
        private BitmapSource _actualPhotoTemplate;
        private BitmapSource _cartoonPhotoTemplate;
        private TemplateMetadata _templateMetadata;
        private PhotoUploadService _uploadService;
        private SessionMonitorService _sessionMonitor; // New Service

        // Fields to store the final rendered bitmaps with QR codes
        private BitmapSource _finalActualTemplate;
        private BitmapSource _finalCartoonTemplate;

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public PrintWindow(BitmapSource actualPhoto, BitmapSource cartoonPhoto, TemplateMetadata metadata = null)
        {
            InitializeComponent();
            
            _actualPhotoTemplate = actualPhoto;
            _cartoonPhotoTemplate = cartoonPhoto;
            _templateMetadata = metadata ?? new TemplateMetadata();
            _uploadService = new PhotoUploadService();
            _sessionMonitor = new SessionMonitorService();
            
            Loaded += PrintWindow_Loaded;
        }
        
        private void PrintWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load background and button images
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            
            var bgPath = Path.Combine(assetsPath, "mainbg.png");
            if (File.Exists(bgPath))
            {
                if (this.FindName("BackgroundImage") is System.Windows.Controls.Image bgImage)
                {
                    bgImage.Source = new BitmapImage(new Uri(bgPath));
                }
            }
            
            var printPath = Path.Combine(assetsPath, "big_PRINT.png");
            if (File.Exists(printPath))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (this.FindName("PrintButton") is Button printButton)
                    {
                        printButton.ApplyTemplate();
                        if (printButton.Template.FindName("PrintButtonImage", printButton) is System.Windows.Controls.Image printButtonImage)
                {
                            printButtonImage.Source = new BitmapImage(new Uri(printPath));
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }

            // Set source and dimensions for photos
            // Only display the first half (one strip) in UI if it's a double-strip composite
            if (_actualPhotoTemplate != null)
            {
                // Check if it's a 4x6 composite made of 2 strips (1200x1800)
                // We want to show only ONE strip in UI to avoid confusion
                if (_actualPhotoTemplate.PixelWidth == 1200 && _actualPhotoTemplate.PixelHeight == 1800 && _templateMetadata?.Resolution?.Width == 600)
                {
                    // Crop left half for display
                    var cropped = new CroppedBitmap(_actualPhotoTemplate, new Int32Rect(0, 0, 600, 1800));
                    ActualPhotoImage.Source = cropped;
                    ActualPhotoGrid.Width = 600;
                    ActualPhotoGrid.Height = 1800;
            }
            else
            {
                    ActualPhotoImage.Source = _actualPhotoTemplate;
                    ActualPhotoGrid.Width = _actualPhotoTemplate.PixelWidth;
                    ActualPhotoGrid.Height = _actualPhotoTemplate.PixelHeight;
                }
            }

            if (_cartoonPhotoTemplate != null)
            {
                if (_cartoonPhotoTemplate.PixelWidth == 1200 && _cartoonPhotoTemplate.PixelHeight == 1800 && _templateMetadata?.Resolution?.Width == 600)
                {
                    // Crop left half for display
                    var cropped = new CroppedBitmap(_cartoonPhotoTemplate, new Int32Rect(0, 0, 600, 1800));
                    CartoonPhotoImage.Source = cropped;
                    CartoonPhotoGrid.Width = 600;
                    CartoonPhotoGrid.Height = 1800;
                }
                else
                {
                    CartoonPhotoImage.Source = _cartoonPhotoTemplate;
                    CartoonPhotoGrid.Width = _cartoonPhotoTemplate.PixelWidth;
                    CartoonPhotoGrid.Height = _cartoonPhotoTemplate.PixelHeight;
                }
            }

            // Start Background Process Immediately
            InitializeSessionAsync();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PerformPrint();
        }

        private void PrintingOption_Checked(object sender, RoutedEventArgs e)
        {
            var checkedBox = sender as CheckBox;
            
            if (checkedBox == Option1CheckBox)
            {
                Option2CheckBox.IsChecked = false;
                Option3CheckBox.IsChecked = false;
            }
            else if (checkedBox == Option2CheckBox)
            {
                Option1CheckBox.IsChecked = false;
                Option3CheckBox.IsChecked = false;
            }
            else if (checkedBox == Option3CheckBox)
            {
                Option1CheckBox.IsChecked = false;
                Option2CheckBox.IsChecked = false;
            }

            UpdatePrintButtonState();
        }

        private void PrintingOption_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdatePrintButtonState();
        }

        private void UpdatePrintButtonState()
        {
            // Only enable if loading is done AND an option is selected
            bool isOptionSelected = Option1CheckBox.IsChecked == true || 
                Option2CheckBox.IsChecked == true || 
                Option3CheckBox.IsChecked == true;

            PrintButton.IsEnabled = isOptionSelected && LoadingOverlay.Visibility == Visibility.Collapsed;
        }

        // This runs automatically on load
        private async void InitializeSessionAsync()
        {
            // Show Loading
            LoadingOverlay.Visibility = Visibility.Visible;
            PrintButton.IsEnabled = false;

            try
            {
                // 1. Save photos to temp folder
                var tempDir = Path.Combine(Path.GetTempPath(), "BangaSession_" + Guid.NewGuid().ToString());
                var filesDir = Path.Combine(tempDir, "files");
                Directory.CreateDirectory(filesDir);

                var actualPath = Path.Combine(filesDir, "photo_original.png");
                var cartoonPath = Path.Combine(filesDir, "photo_ai.png");
                var zipPath = Path.Combine(tempDir, "session.zip");

                // Determine what to upload:
                // For Strip mode (600x1800 template), the _actualPhotoTemplate is a combined 1200x1800 image
                // We should upload only ONE strip (600x1800), not the combined image
                BitmapSource uploadActual = _actualPhotoTemplate;
                BitmapSource uploadCartoon = _cartoonPhotoTemplate;
                
                bool isStripMode = _templateMetadata?.Resolution?.Width == 600 && _templateMetadata?.Resolution?.Height == 1800;
                
                if (isStripMode && _actualPhotoTemplate != null && _actualPhotoTemplate.PixelWidth == 1200)
        {
                    // Crop to get just the left strip (single 600x1800)
                    uploadActual = new CroppedBitmap(_actualPhotoTemplate, new Int32Rect(0, 0, 600, 1800));
                    System.Diagnostics.Debug.WriteLine("Strip Mode Upload: Cropping to single 600x1800 strip");
                }
                
                if (isStripMode && _cartoonPhotoTemplate != null && _cartoonPhotoTemplate.PixelWidth == 1200)
                {
                    // Crop to get just the left strip (single 600x1800)
                    uploadCartoon = new CroppedBitmap(_cartoonPhotoTemplate, new Int32Rect(0, 0, 600, 1800));
                }

                SaveBitmapToFile(uploadActual, actualPath);
                SaveBitmapToFile(uploadCartoon, cartoonPath);

                // 2. Create Zip
                if (File.Exists(actualPath) || File.Exists(cartoonPath))
        {
                     ZipFile.CreateFromDirectory(filesDir, zipPath);
                }

                // 3. Upload
                var response = await _uploadService.UploadSessionAsync(actualPath, cartoonPath, File.Exists(zipPath) ? zipPath : null);

                if (response != null && response.Ok && !string.IsNullOrEmpty(response.Link))
        {
                    System.Diagnostics.Debug.WriteLine($"Upload success! Link: {response.Link}");
                    
                    // 4. Generate QR
                    var qrImage = GenerateQrCode(response.Link);

                    // 5. Place QR on Template (Must be on UI Thread)
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        if (_templateMetadata?.QrSlot != null)
                        {
                             AddQrToCanvas(ActualPhotoCanvas, qrImage, _templateMetadata.QrSlot);
                             AddQrToCanvas(CartoonPhotoCanvas, qrImage, _templateMetadata.QrSlot);
                             
                             // Force layout update
                             ActualPhotoGrid.UpdateLayout();
                             CartoonPhotoGrid.UpdateLayout();

                             // Small delay to allow UI to refresh
                             await Task.Delay(500);

                             // 6. Capture the final look with QR codes into new bitmaps
                             _finalActualTemplate = RenderGridToBitmap(ActualPhotoGrid);
                             _finalCartoonTemplate = RenderGridToBitmap(CartoonPhotoGrid);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("WARNING: No QR Slot defined in metadata!");
                            // Fallback if no QR slot
                            _finalActualTemplate = _actualPhotoTemplate;
                            _finalCartoonTemplate = _cartoonPhotoTemplate;
                        }
                    });
                }
                else
                {
                     // SHOW ERROR for debugging
                     string errorMsg = response?.Message ?? "Unknown Error";
                     System.Diagnostics.Debug.WriteLine($"Failed to upload session or generate QR: {errorMsg}");
                     MessageBox.Show($"Upload Failed. Check internet/server.\nDetails: {errorMsg}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                     _finalActualTemplate = _actualPhotoTemplate;
                     _finalCartoonTemplate = _cartoonPhotoTemplate;
                }

                // Clean up temp files (fire and forget)
                try 
                { 
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); 
                } 
                catch { }

            }
            catch (Exception ex)
            {
                // Log error but don't stop printing
                System.Diagnostics.Debug.WriteLine($"Upload/QR Error: {ex.Message}");
                _finalActualTemplate = _actualPhotoTemplate;
                _finalCartoonTemplate = _cartoonPhotoTemplate;
            }
            finally
            {
                // Hide loading overlay
                await Dispatcher.InvokeAsync(() => 
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    UpdatePrintButtonState(); // Enable button if option is selected
                });
            }
        }

        private async void PerformPrint()
        {
             // Disable button to prevent double clicks
             PrintButton.IsEnabled = false;

             try
             {
                string printDescription = "";
                BitmapSource imageToPrint = null;
                int copiesToPrint = 1;

                // Determine if we are in Strip Mode (600x1800) or 4R Mode (1200x1800)
                bool isStripMode = _templateMetadata?.Resolution?.Width == 600;

                if (isStripMode)
                {
                    // STRIP MODE (2x6 cut)
                    // ...
                    if (Option1CheckBox.IsChecked == true)
                    {
                        printDescription = "2 Original Photos (Strip)";
                        // Original + Original
                        imageToPrint = CombineBitmapsSideBySide(_finalActualTemplate, _finalActualTemplate);
                    }
                    else if (Option2CheckBox.IsChecked == true)
                    {
                        printDescription = "1 Original + 1 AI (Strip)";
                        // Original + AI
                        imageToPrint = CombineBitmapsSideBySide(_finalActualTemplate, _finalCartoonTemplate);
                    }
                    else if (Option3CheckBox.IsChecked == true)
                    {
                        printDescription = "2 AI Photos (Strip)";
                        // AI + AI
                        imageToPrint = CombineBitmapsSideBySide(_finalCartoonTemplate, _finalCartoonTemplate);
                    }
                }
                else
                {
                    // 4R MODE (4x6)
            
            if (Option1CheckBox.IsChecked == true)
            {
                        printDescription = "2 Original Photos (4R)";
                        imageToPrint = _finalActualTemplate;
                        copiesToPrint = 2;
            }
            else if (Option2CheckBox.IsChecked == true)
            {
                        printDescription = "1 Original + 1 AI (4R)";
                        
                        // Print Original First (4R Mode -> enableCut = false)
                        PrintSingleImage("Original 4R", 1, _finalActualTemplate, false);
                        
                        // Print AI Second (4R Mode -> enableCut = false)
                        PrintSingleImage("AI 4R", 1, _finalCartoonTemplate, false);
                        
                        // Skip the common PrintPhotos call below
                        await FinishPrintingAsync();
                        return;
            }
            else if (Option3CheckBox.IsChecked == true)
            {
                        printDescription = "2 AI Photos (4R)";
                        imageToPrint = _finalCartoonTemplate;
                        copiesToPrint = 2;
            }
                }

                if (imageToPrint != null)
                {
                    // Pass isStripMode as enableCut argument
                    PrintSingleImage(printDescription, copiesToPrint, imageToPrint, isStripMode);
                }
                
                await FinishPrintingAsync();
             }
             catch (Exception printEx)
             {
                PrintButton.IsEnabled = true;
                MessageBox.Show($"Printing Error: {printEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }

        private async Task FinishPrintingAsync()
        {
                // Wait for 10 seconds
                await Task.Delay(10000);
            
                // DECREMENT SESSION (Use one session)
                await _sessionMonitor.DecrementSessionAsync();
                
                // CHECK REMAINING SESSIONS
                int remaining = await _sessionMonitor.GetRemainingSessionsAsync();

                if (remaining > 0)
                {
                    // Still have sessions, go to Welcome Screen
            var mainWindow = new MainWindow();
            mainWindow.Show();
                }
                else
                {
                    // No sessions left, go to Lock Screen
                    var otpWindow = new OtpWindow();
                    otpWindow.Show();
                }
                
            this.Close();
        }

        private BitmapSource CombineBitmapsSideBySide(BitmapSource left, BitmapSource right)
        {
            int width = 1200;
            int height = 1800;
            int stride = width * 4; // 4 bytes per pixel (Bgra32)
            byte[] pixels = new byte[height * stride];

            // Helper to copy pixels
            void CopyPixels(BitmapSource source, int targetX)
            {
                if (source == null) return;
                
                // Ensure source is 600x1800 and PBGRA32
                var converted = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
                int sourceStride = 600 * 4;
                byte[] sourcePixels = new byte[1800 * sourceStride];
                converted.CopyPixels(sourcePixels, sourceStride, 0);

                for (int y = 0; y < 1800; y++)
                {
                    int targetIndex = (y * stride) + (targetX * 4);
                    int sourceIndex = y * sourceStride;
                    Array.Copy(sourcePixels, sourceIndex, pixels, targetIndex, sourceStride);
                }
            }

            CopyPixels(left, 0);   // Left half (0-599)
            CopyPixels(right, 600); // Right half (600-1199)

            var combined = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
            combined.Freeze();
            return combined;
        }

        private void PrintSingleImage(string description, int copies, BitmapSource image, bool enableCut)
        {
            // --- SIMULATION MODE CONFIGURATION ---
            bool isSimulationMode = BangaConfig.Current.PrinterSimulationMode;
            // -------------------------------------

            if (isSimulationMode)
            {
                try
                {
                    string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string simPath = Path.Combine(docsPath, "Banga_Prints");
                    if (!Directory.Exists(simPath)) Directory.CreateDirectory(simPath);

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    for (int i = 0; i < copies; i++)
                    {
                        string fileName = $"Print_{timestamp}_{description}_Copy{i+1}_Cut{enableCut}.png";
                        string fullPath = Path.Combine(simPath, fileName);
                        SaveBitmapToFile(image, fullPath);
                    }
                    System.Diagnostics.Process.Start("explorer.exe", simPath);
                }
                catch (Exception ex) { MessageBox.Show($"Simulation Error: {ex.Message}"); }
            }
            else
            {
                try
                {
                    PrintDialog printDialog = new PrintDialog();
                    
                    // AUTO-SELECT PRINTER based on Cut mode
                    // enableCut = true  -> Strip Mode -> Use SelectedPrinterStrip (Cut ON)
                    // enableCut = false -> 4R Mode    -> Use SelectedPrinter4R (Cut OFF)
                    string printerName;
                    if (enableCut)
                    {
                        printerName = BangaConfig.Current.SelectedPrinterStrip;
                        System.Diagnostics.Debug.WriteLine($"Strip Mode: Using printer '{printerName}'");
                    }
                    else
                    {
                        printerName = BangaConfig.Current.SelectedPrinter4R;
                        System.Diagnostics.Debug.WriteLine($"4R Mode: Using printer '{printerName}'");
                    }
                    
                    // Fallback to legacy single printer if new settings not configured
                    if (string.IsNullOrEmpty(printerName))
                    {
                        printerName = BangaConfig.Current.SelectedPrinter;
                        System.Diagnostics.Debug.WriteLine($"Fallback: Using legacy printer '{printerName}'");
                    }
                    
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        printDialog.PrintQueue = new System.Printing.PrintServer().GetPrintQueue(printerName);
                    }

                    // --- PRINT TICKET CONFIGURATION ---
                    // Since we're using 2 printer profiles with pre-configured settings:
                    // - Strip Printer: Driver set to "4x6 (2 inch cut)"
                    // - 4R Printer: Driver set to "4x6" (No Cut)
                    // We just use the printer's default settings.
                    
                    PrintTicket ticket = printDialog.PrintTicket;
                    PrintCapabilities capabilities = null;
                    try { capabilities = printDialog.PrintQueue.GetPrintCapabilities(ticket); } catch { }
                    
                    System.Diagnostics.Debug.WriteLine($"Using printer: {printerName}, enableCut: {enableCut}");
                    
                    // Apply the ticket (use printer defaults)
                    printDialog.PrintTicket = ticket;

                    for (int i = 0; i < copies; i++)
                    {
                        // Re-query capabilities with the updated ticket for accurate printable area
                        try { capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket); } catch {}

                        double printableWidth = capabilities?.PageImageableArea?.ExtentWidth ?? printDialog.PrintableAreaWidth;
                        double printableHeight = capabilities?.PageImageableArea?.ExtentHeight ?? printDialog.PrintableAreaHeight;
                        
                        if (printableWidth <= 0 || printableHeight <= 0)
                        {
                            printableWidth = 6.0 * 96.0; 
                            printableHeight = 4.0 * 96.0;
                        }

                        var vis = new DrawingVisual();
                        using (var dc = vis.RenderOpen())
                        {
                            dc.DrawImage(image, new System.Windows.Rect(0, 0, printableWidth, printableHeight));
                        }
                        printDialog.PrintVisual(vis, $"Banga Photo - {description}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Printer Error: {ex.Message}");
                }
            }
        }

        private void SaveBitmapToFile(BitmapSource bitmap, string path)
        {
            if (bitmap == null) return;
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                
                // Inject Metadata (Invisible Watermark)
                // Note: PNG supports textual metadata via tEXt/zTXt chunks.
                BitmapMetadata metadata = new BitmapMetadata("png");
                metadata.SetQuery("/Text/Software", "Banga Photobooth");
                metadata.SetQuery("/Text/Copyright", "© 2025 Banga Photobooth. All Rights Reserved.");
                metadata.SetQuery("/Text/Author", "Banga Photobooth");
                
                var frame = BitmapFrame.Create(bitmap, null, metadata, null);
                encoder.Frames.Add(frame);
                encoder.Save(fileStream);
            }
        }

        private BitmapSource GenerateQrCode(string content)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (System.Drawing.Bitmap qrBitmap = qrCode.GetGraphic(20))
                    {
                         var hBitmap = qrBitmap.GetHbitmap();
                         try
                         {
                             var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                 hBitmap,
                                 IntPtr.Zero,
                                 Int32Rect.Empty,
                                 BitmapSizeOptions.FromEmptyOptions());
                             source.Freeze();
                             return source;
                         }
                         finally
                         {
                             DeleteObject(hBitmap);
                         }
                    }
                }
            }
        }

        private void AddQrToCanvas(Canvas canvas, BitmapSource qrImage, Slot slot)
        {
            if (slot == null) return;

            var image = new System.Windows.Controls.Image
            {
                Source = qrImage,
                Width = slot.Width,
                Height = slot.Height,
                Stretch = Stretch.Uniform
            };

            Canvas.SetLeft(image, slot.X);
            Canvas.SetTop(image, slot.Y);
            
            // Ensure QR is on top
            Panel.SetZIndex(image, 999);
            
            canvas.Children.Add(image);
            System.Diagnostics.Debug.WriteLine($"Added QR to canvas at {slot.X},{slot.Y} size {slot.Width}x{slot.Height}");
        }

        // Removed RenderGridToBitmap if not used, or kept if needed elsewhere. 
        // Removed PrintPhotos as it is replaced by PrintSingleImage.
        
        private BitmapSource RenderGridToBitmap(Grid grid)
        {
            // Grid must be measured and arranged
            var size = new System.Windows.Size(grid.Width, grid.Height);
            grid.Measure(size);
            grid.Arrange(new System.Windows.Rect(size));
            grid.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)grid.Width, 
                (int)grid.Height, 
                96, 
                96, 
                PixelFormats.Pbgra32);

            rtb.Render(grid);
            rtb.Freeze();
            return rtb;
        }
    }
}
