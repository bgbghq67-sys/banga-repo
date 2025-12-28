using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Banga_Photobooth
{
    public partial class PreviewWindow : System.Windows.Window
    {
        private readonly List<ImageSource> capturedPhotos;
        private readonly BitmapImage? selectedTemplate;
        private readonly TemplateMetadata? selectedTemplateMetadata;
        private readonly List<ImageSource?> selectedPhotos = new() { null, null, null, null };
        private readonly List<Border> photoTiles = new();
        private static InferenceSession? _onnxSession;
        private static string? _onnxInputName;
        
        // Use temp folder for debug logs to avoid permission issues in Program Files
        private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "banga_preview_debug.log");
        
        public PreviewWindow(List<ImageSource> photos, BitmapImage? template = null, TemplateMetadata? metadata = null)
        {
            InitializeComponent();

            var log = "=== PreviewWindow CONSTRUCTOR ===\n";
            log += $"photos count: {photos?.Count ?? 0}\n";
            log += $"template passed: {template != null}\n";
            log += $"metadata passed: {metadata != null}\n";
            if (template != null)
            {
                log += $"template URI: {template.UriSource}\n";
            }
            if (metadata != null)
            {
                log += $"metadata template name: {metadata.TemplateName}\n";
                log += $"metadata resolution: {metadata.Resolution?.Width}x{metadata.Resolution?.Height}\n";
            }
            System.Diagnostics.Debug.WriteLine(log);
            System.IO.File.AppendAllText(DebugLogPath, log + "\n");

            capturedPhotos = photos ?? new List<ImageSource>();
            selectedTemplate = template;
            selectedTemplateMetadata = metadata;
            
            Loaded += PreviewWindow_Loaded;
        }

        private void PreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== PreviewWindow_Loaded START ===");
            
            // Load images from file paths
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            var bgPath = Path.Combine(assetsPath, "mainbg.png");
            if (File.Exists(bgPath))
            {
                var bgBrush = new ImageBrush(new BitmapImage(new Uri(bgPath)));
                bgBrush.Stretch = Stretch.UniformToFill;
                ((Grid)Content).Background = bgBrush;
            }
            
            var nextPath = Path.Combine(assetsPath, "big_NEXT.png");
            if (File.Exists(nextPath))
            {
                // Load NEXT button image (must access through template - delay until after render)
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var nextButton = this.FindName("NextButton") as Button;
                    if (nextButton != null)
                    {
                        // Ensure template is applied
                        if (!nextButton.IsLoaded)
                            nextButton.Loaded += (s, e) => LoadNextButtonImage(nextButton, nextPath);
                        else
                            LoadNextButtonImage(nextButton, nextPath);
                    }
                }), DispatcherPriority.Loaded);
            }
            
            LoadTemplates();
            RenderPhotoGrid();
            UpdateTemplatePreview();
            System.Diagnostics.Debug.WriteLine("=== PreviewWindow_Loaded END ===");
        }
        
        private void LoadNextButtonImage(Button nextButton, string imagePath)
        {
            // Apply template first
            nextButton.ApplyTemplate();
            
            // Find image inside the template
            var nextButtonImage = nextButton.Template?.FindName("NextButtonImage", nextButton) as Image;
            if (nextButtonImage != null)
            {
                nextButtonImage.Source = new BitmapImage(new Uri(imagePath));
            }
            else
            {
                // Fallback: search visual tree
                var image = FindVisualChild<Image>(nextButton);
                if (image != null)
                {
                    image.Source = new BitmapImage(new Uri(imagePath));
                }
            }
        }
        
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void LoadTemplates()
        {
            var log = "=== LoadTemplates() ===\n";
            log += $"selectedTemplate is null: {selectedTemplate == null}\n";
            log += $"selectedTemplateMetadata is null: {selectedTemplateMetadata == null}\n";
            
            if (selectedTemplate != null)
            {
                log += $"Setting template image source: {selectedTemplate.UriSource}\n";
                StandardTemplateImage.Source = selectedTemplate;
                CartoonTemplateImage.Source = selectedTemplate;
                log += $"StandardTemplateImage.Source set: {StandardTemplateImage.Source != null}\n";
                log += $"CartoonTemplateImage.Source set: {CartoonTemplateImage.Source != null}\n";
            }
            else
            {
                log += "WARNING: selectedTemplate is NULL!\n";
            }

            // Set dimensions based on template metadata
            if (selectedTemplateMetadata?.Resolution != null)
            {
                double width = selectedTemplateMetadata.Resolution.Width;
                double height = selectedTemplateMetadata.Resolution.Height;

                log += $"Setting dimensions from metadata: {width}x{height}\n";

                // Set Grid dimensions
                StandardTemplateSurface.Width = width;
                StandardTemplateSurface.Height = height;
                CartoonTemplateSurface.Width = width;
                CartoonTemplateSurface.Height = height;

                // Set Image dimensions
                StandardTemplateImage.Width = width;
                StandardTemplateImage.Height = height;
                CartoonTemplateImage.Width = width;
                CartoonTemplateImage.Height = height;

                // Set Canvas dimensions
                StandardTemplateOverlay.Width = width;
                StandardTemplateOverlay.Height = height;
                CartoonTemplateOverlay.Width = width;
                CartoonTemplateOverlay.Height = height;

                log += $"Dimensions set - StandardTemplateSurface: {StandardTemplateSurface.Width}x{StandardTemplateSurface.Height}\n";
                log += $"Dimensions set - StandardTemplateImage: {StandardTemplateImage.Width}x{StandardTemplateImage.Height}\n";
            }
            else
            {
                log += "WARNING: No metadata resolution - using default dimensions from XAML\n";
            }
            
            System.Diagnostics.Debug.WriteLine(log);
            System.IO.File.AppendAllText(DebugLogPath, log + "\n");
        }

        private void RenderPhotoGrid()
        {
            PhotoGrid.Children.Clear();
            photoTiles.Clear();

            for (int i = 0; i < capturedPhotos.Count && i < 6; i++)
            {
                var photo = capturedPhotos[i];
                
                var border = new Border
                {
                    Background = new ImageBrush(photo)
                    {
                        Stretch = Stretch.UniformToFill,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    },
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                    BorderThickness = new Thickness(3),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    Tag = i // Store index
                };

                border.MouseDown += PhotoTile_Click;
                photoTiles.Add(border);
                PhotoGrid.Children.Add(border);
            }
        }

        private void PhotoTile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not int photoIndex)
                return;

            // Check if this photo is already selected
            int existingSlot = selectedPhotos.IndexOf(capturedPhotos[photoIndex]);
            
            if (existingSlot >= 0)
            {
                // Deselect: remove from slot
                selectedPhotos[existingSlot] = null;
                UpdatePhotoTileAppearance(photoIndex, false);
            }
            else
            {
                // Select: add to next available slot
                int nextSlot = selectedPhotos.IndexOf(null);
                if (nextSlot >= 0)
                {
                    selectedPhotos[nextSlot] = capturedPhotos[photoIndex];
                    UpdatePhotoTileAppearance(photoIndex, true);
                }
                // If all slots full, do nothing (or replace last one)
            }

            UpdateTemplatePreview();
            UpdateNextButton();
        }

        private void UpdatePhotoTileAppearance(int photoIndex, bool isSelected)
        {
            if (photoIndex < 0 || photoIndex >= photoTiles.Count)
                return;

            var border = photoTiles[photoIndex];
            
            if (isSelected)
            {
                // Highlight selected photos
                border.BorderBrush = new SolidColorBrush(Colors.Red);
                border.BorderThickness = new Thickness(6); // Increased thickness (5 -> 6)
                border.Opacity = 1.0;
            }
            else
            {
                // Reset to default
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                border.BorderThickness = new Thickness(3);
                border.Opacity = 1.0;
            }
        }

        private void UpdateTemplatePreview()
        {
            // Clear existing photo overlays
            ClearPhotoOverlays(StandardTemplateOverlay);
            ClearPhotoOverlays(CartoonTemplateOverlay);

            // Composite selected photos only into the visible template.
            CompositePhotosIntoTemplate(StandardTemplateOverlay, selectedPhotos, false);
        }

        private void ClearPhotoOverlays(Canvas overlay)
        {
            overlay.Children.Clear();
        }

        private void CompositePhotosIntoTemplate(Canvas overlay, List<ImageSource?> photos, bool applyCartoon)
        {
            var photoCount = photos.Count(p => p != null);
            var log = $"=== CompositePhotosIntoTemplate (Cartoon={applyCartoon}) ===\n";
            log += $"Total photos in list: {photos.Count}\n";
            log += $"Non-null photos: {photoCount}\n";
            log += $"PhotoSlots count: {selectedTemplateMetadata?.PhotoSlots?.Count}\n";
            for (int i = 0; i < photos.Count; i++)
            {
                log += $"  photos[{i}] is null: {photos[i] == null}\n";
            }
            System.IO.File.AppendAllText(DebugLogPath, log);
            
            if (photoCount == 0) return;

            // Check if we have metadata with photo slots
            if (selectedTemplateMetadata?.PhotoSlots == null || selectedTemplateMetadata.PhotoSlots.Count == 0)
            {
                return; // Skip if no metadata
            }

            // Position photos according to metadata slots (no scaling needed - Viewbox handles it)
            int photoIndex = 0;
            foreach (var slot in selectedTemplateMetadata.PhotoSlots)
            {
                log = $"Slot #{photoIndex}: photoIndex={photoIndex}, photos.Count={photos.Count}\n";
                System.IO.File.AppendAllText(DebugLogPath, log);
                
                if (photoIndex >= photos.Count)
                {
                    System.IO.File.AppendAllText(DebugLogPath, "  Breaking: photoIndex >= photos.Count\n");
                    break;
                }

                var photo = photos[photoIndex];
                if (photo == null)
                {
                    System.IO.File.AppendAllText(DebugLogPath, $"  Skipping: photo is null at index {photoIndex}\n");
                    photoIndex++;
                    continue;
                }

                ImageSource displayPhoto = photo;

                // Apply cartoon filter if needed
                if (applyCartoon)
                {
                    displayPhoto = ApplyCartoonFilter(photo);
                }

                // Create border with image brush for the photo - use exact slot dimensions
                var photoContainer = new Border
                {
                    Width = slot.Width,
                    Height = slot.Height,
                    Background = new ImageBrush(displayPhoto)
                    {
                        Stretch = Stretch.UniformToFill,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    },
                    ClipToBounds = true
                };

                // Position using Canvas attached properties - use exact slot coordinates
                Canvas.SetLeft(photoContainer, slot.X);
                Canvas.SetTop(photoContainer, slot.Y);

                overlay.Children.Add(photoContainer);
                log = $"  Added photo to slot at ({slot.X}, {slot.Y}) size ({slot.Width}x{slot.Height})\n";
                System.IO.File.AppendAllText(DebugLogPath, log);
                
                photoIndex++;
            }
            
            log = $"Total photos added to overlay: {overlay.Children.Count}\n";
            System.IO.File.AppendAllText(DebugLogPath, log + "\n");
        }

        private void CompositePhotosGeneric(Canvas overlay, List<ImageSource?> photos, bool applyCartoon)
        {
            // Generic fallback layout for templates without metadata
            overlay.Children.Clear();

            double overlayWidth = overlay.ActualWidth;
            double overlayHeight = overlay.ActualHeight;

            if (overlayWidth <= 0 || overlayHeight <= 0)
            {
                overlay.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CompositePhotosGeneric(overlay, photos, applyCartoon);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            // Calculate photo positions (vertical strip layout)
            double slotHeight = overlayHeight / 4.5;
            double slotWidth = slotHeight * 0.75; // 3:4 aspect ratio
            double startY = 50;
            double spacing = (overlayHeight - startY * 2 - slotHeight * 4) / 3;

            for (int i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                if (photo == null) continue;

                ImageSource displayPhoto = photo;

                if (applyCartoon)
                {
                    displayPhoto = ApplyCartoonFilter(photo);
                }

                var photoImage = new Image
                {
                    Source = displayPhoto,
                    Width = slotWidth,
                    Height = slotHeight,
                    Stretch = Stretch.UniformToFill
                };

                Canvas.SetLeft(photoImage, (overlayWidth - slotWidth) / 2);
                Canvas.SetTop(photoImage, startY + i * (slotHeight + spacing));

                overlay.Children.Add(photoImage);
            }
        }

        private ImageSource ApplyCartoonFilter(ImageSource source)
        {
            try
            {
                // Lazy load ONNX model
                if (_onnxSession == null)
                {
                    string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AnimeGANv3_large_Ghibli_c1_e299.onnx");
                    System.Diagnostics.Debug.WriteLine($"Attempting to load model from: {modelPath}");
                    System.IO.File.AppendAllText(DebugLogPath, $"Attempting to load ONNX model from: {modelPath}\n");
                    
                    if (File.Exists(modelPath))
                    {
                        _onnxSession = new InferenceSession(modelPath);
                        _onnxInputName = _onnxSession.InputMetadata.Keys.FirstOrDefault();
                        var outputName = _onnxSession.OutputMetadata.Keys.FirstOrDefault();
                        
                        System.Diagnostics.Debug.WriteLine("ONNX model loaded successfully!");
                        System.Diagnostics.Debug.WriteLine($"Input name: {_onnxInputName}");
                        System.Diagnostics.Debug.WriteLine($"Output name: {outputName}");
                        System.IO.File.AppendAllText(DebugLogPath, $"ONNX model loaded! Input: {_onnxInputName}, Output: {outputName}\n");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ONNX model not found at: {modelPath}");
                        System.IO.File.AppendAllText(DebugLogPath, $"ONNX model NOT FOUND at: {modelPath}\n");
                        return source;
                    }
                }

                // Convert ImageSource to BitmapSource
                if (source is not BitmapSource bitmapSource)
                    return source;

            // Convert to Mat for preprocessing
            using var mat = bitmapSource.ToMat();
            
            // Get original dimensions
            int origHeight = mat.Height;
            int origWidth = mat.Width;
            
            // Resize to multiples of 8 for better processing (matching Python code)
            int newWidth = origWidth < 256 ? 256 : origWidth - (origWidth % 8);
            int newHeight = origHeight < 256 ? 256 : origHeight - (origHeight % 8);
            
            using var resized = new Mat();
            Cv2.Resize(mat, resized, new OpenCvSharp.Size(newWidth, newHeight));
                
            // Convert BGR to RGB
            using var rgb = new Mat();
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);
                
            // Normalize to [-1, 1] range (matching Python: /127.5 - 1.0)
            using var normalized = new Mat();
            rgb.ConvertTo(normalized, MatType.CV_32FC3, 1.0 / 127.5, -1.0);
            
            // Create tensor in NHWC format (1, height, width, 3) - what AnimeGAN expects
            var inputTensor = new DenseTensor<float>(new[] { 1, newHeight, newWidth, 3 });
            
            // Fill tensor with normalized pixel values
            normalized.GetArray(out Vec3f[] pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                int y = i / newWidth;
                int x = i % newWidth;
                // NHWC format: [batch, height, width, channel]
                inputTensor[0, y, x, 0] = pixels[i].Item0; // R
                inputTensor[0, y, x, 1] = pixels[i].Item1; // G
                inputTensor[0, y, x, 2] = pixels[i].Item2; // B
            }
                
                // Run inference (use actual input name from model)
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(_onnxInputName ?? "input", inputTensor)
                };
                
            using var results = _onnxSession.Run(inputs);
            var output = results.First().AsTensor<float>();
            
            // Get output dimensions from tensor
            int outHeight = output.Dimensions[1];
            int outWidth = output.Dimensions[2];
            
            // Convert output back to image (NHWC format)
            var outputMat = new Mat(outHeight, outWidth, MatType.CV_32FC3);
            var outputData = new Vec3f[outHeight * outWidth];
            
            for (int y = 0; y < outHeight; y++)
            {
                for (int x = 0; x < outWidth; x++)
                {
                    // Denormalize from [-1, 1] to [0, 1] range: (x + 1) / 2
                    float r = (output[0, y, x, 0] + 1.0f) / 2.0f;
                    float g = (output[0, y, x, 1] + 1.0f) / 2.0f;
                    float b = (output[0, y, x, 2] + 1.0f) / 2.0f;
                    
                    // Clamp to [0, 1]
                    r = Math.Max(0, Math.Min(1, r));
                    g = Math.Max(0, Math.Min(1, g));
                    b = Math.Max(0, Math.Min(1, b));
                    
                    outputData[y * outWidth + x] = new Vec3f(r, g, b);
                }
            }
            outputMat.SetArray(outputData);
                
            // Convert to 0-255 range
            using var denormalized = new Mat();
            outputMat.ConvertTo(denormalized, MatType.CV_8UC3, 255.0);
                
            // Convert RGB back to BGR
            using var bgr = new Mat();
            Cv2.CvtColor(denormalized, bgr, ColorConversionCodes.RGB2BGR);
            
            // Resize back to original dimensions
            using var final = new Mat();
            Cv2.Resize(bgr, final, new OpenCvSharp.Size(origWidth, origHeight));
            
            // Use 100% anime filter output (no blending)
            var result = final.ToBitmapSource();
            result.Freeze();
            return result;
            }
            catch (Exception ex)
            {
                var errorMsg = $"!!! ANIME FILTER ERROR !!!\nMessage: {ex.Message}\nStack: {ex.StackTrace}\n";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                System.IO.File.AppendAllText(DebugLogPath, errorMsg);
                // If filtering fails, return original
                return source;
            }
        }

        private void UpdateNextButton()
        {
            int selectedCount = selectedPhotos.Count(p => p != null);
            NextButton.IsEnabled = (selectedCount == 4);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Build the cartoon overlay only when advancing, keeping the preview snappy.
            PrepareCartoonOverlayForPrint();

            // Prepare final composited images for printing
            var standardStrip = CreateFinalComposite(StandardTemplateSurface);
            var cartoonStrip = CreateFinalComposite(CartoonTemplateSurface);

            // Navigate to PrintWindow
            var printWindow = new PrintWindow(standardStrip, cartoonStrip, selectedTemplateMetadata);
            printWindow.Show();
            this.Close();
        }

        private void PrepareCartoonOverlayForPrint()
        {
            ClearPhotoOverlays(CartoonTemplateOverlay);
            CompositePhotosIntoTemplate(CartoonTemplateOverlay, selectedPhotos, true);
        }

        private BitmapSource CreateFinalComposite(Grid templateSurface)
        {
            try
            {
                // Get the actual dimensions from the surface
                double width = templateSurface.Width;
                double height = templateSurface.Height;

                // Ensure the surface is properly arranged and rendered
                templateSurface.Measure(new System.Windows.Size(width, height));
                templateSurface.Arrange(new System.Windows.Rect(0, 0, width, height));
                templateSurface.UpdateLayout();

                // Create a render target at the template's resolution
                var renderTarget = new RenderTargetBitmap(
                    (int)width,
                    (int)height,
                    96, // DPI
                    96,
                    PixelFormats.Pbgra32);

                // Render the surface to the bitmap
                renderTarget.Render(templateSurface);
                renderTarget.Freeze();
                
                // --- STRIP LOGIC ---
                // If template width is 600 (2x6 strip), duplicate it to make 1200x1800 (4x6)
                if ((int)width == 600 && (int)height == 1800)
                {
                     System.Diagnostics.Debug.WriteLine("Detected 2x6 Strip. Duplicating for 4x6 output.");
                     
                     // Create a new 4x6 canvas
                     var finalWidth = 1200;
                     var finalHeight = 1800;
                     var drawingVisual = new DrawingVisual();
                     
                     using (var ctx = drawingVisual.RenderOpen())
                     {
                         // Draw white background
                         ctx.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, finalWidth, finalHeight));
                         
                         // Draw strip on Left (0,0)
                         ctx.DrawImage(renderTarget, new System.Windows.Rect(0, 0, 600, 1800));
                         
                         // Draw strip on Right (600,0)
                         ctx.DrawImage(renderTarget, new System.Windows.Rect(600, 0, 600, 1800));
                         
                         // Optional: Draw a very thin gray line in middle for cutting guide?
                         // ctx.DrawLine(new Pen(Brushes.LightGray, 1), new Point(600, 0), new Point(600, 1800));
                     }
                     
                     var finalTarget = new RenderTargetBitmap(finalWidth, finalHeight, 96, 96, PixelFormats.Pbgra32);
                     finalTarget.Render(drawingVisual);
                     finalTarget.Freeze();
                     
                     System.Diagnostics.Debug.WriteLine($"Created final composite (Double Strip): {finalWidth}x{finalHeight}");
                     return finalTarget;
                }

                System.Diagnostics.Debug.WriteLine($"Created final composite (Standard): {width}x{height}");
                return renderTarget;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating final composite: {ex.Message}");
                System.IO.File.AppendAllText(DebugLogPath, $"Composite error: {ex.Message}\n");
                
                // Return a fallback empty bitmap
                var fallback = new RenderTargetBitmap(1200, 1800, 96, 96, PixelFormats.Pbgra32);
                fallback.Freeze();
                return fallback;
            }
        }
    }
}

