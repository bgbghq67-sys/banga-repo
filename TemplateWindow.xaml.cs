using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Banga_Photobooth
{
    public partial class TemplateWindow : Window
    {
        private readonly List<BitmapImage> _templates = new();
        private readonly List<BitmapImage> _templateDisplayImages = new();
        private readonly List<string> _templateNames = new();
        private readonly List<TemplateMetadata> _templateMetadata = new();
        private int _selectedIndex = -1;
        private readonly List<Grid> _templateContainers = new();
        private static readonly Dictionary<string, string> TemplatePreviewMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["black video.png"] = "black video double.png",
            ["blue_check.png"] = "blue_check double.png",
            ["Film_black.png"] = "Fil_black double.png",
            ["1200X1800_H.png"] = "1200x1800_H double.png",
            ["korea_frame.png"] = "korea_frame double.png",
            ["magazine.png"] = "magazine double.png"
        };
        
        // Static simulation mode shared with CaptureWindow - NOW USING BangaConfig
        // NOTE: TemplateWindow only cares about Camera mode for the "Debug" toggle if it existed, 
        // but now we split them. We'll map this property to CameraSimulation for backward compatibility 
        // if anything else accesses it.
        public static bool SimulationMode 
        { 
            get => BangaConfig.Current.CameraSimulationMode; 
            set 
            {
                BangaConfig.Current.CameraSimulationMode = value;
                BangaConfig.Save();
            }
        }

        public TemplateWindow()
        {
            InitializeComponent();
            Loaded += TemplateWindow_Loaded;
        }
        

        private DispatcherTimer _selectionTimer;
        private int _selectionCountdown = 7;
        private bool _isCountdownRunning = false;
        private const double FULL_DASH_ARRAY = 289; // Circumference for path 2*pi*46

        private void TemplateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load background image from file path
            var assetsPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets");
            var bgPath = System.IO.Path.Combine(assetsPath, "mainbg.png");
            if (System.IO.File.Exists(bgPath))
            {
                var mainGrid = Content as Grid;
                if (mainGrid != null)
                {
                    var bgBrush = new ImageBrush(new BitmapImage(new Uri(bgPath)));
                    bgBrush.Stretch = Stretch.UniformToFill;
                    mainGrid.Background = bgBrush;
                }
            }
            
            StartSelectionCountdown();
            
            LoadTemplates();
            RenderTemplatePages();
        }

        private void StartSelectionCountdown()
        {
            _selectionCountdown = 7;
            UpdateCountdownUI();
            
            _selectionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _selectionTimer.Tick += SelectionTimer_Tick;
            _selectionTimer.Start();
            _isCountdownRunning = true;
        }

        private void SelectionTimer_Tick(object sender, EventArgs e)
        {
            _selectionCountdown--;
            UpdateCountdownUI();

            if (_selectionCountdown <= 0)
            {
                _selectionTimer.Stop();
                _isCountdownRunning = false;
                
                // Time's up!
                if (_selectedIndex != -1)
                    {
                    // Selection made, go!
                    NavigateToCapture();
                }
                else
                {
                    // No selection, wait for user
                    SelectionCountdownText.Text = "!";
                    SelectionCountdownText.Foreground = Brushes.Red;
                }
            }
        }

        private void UpdateCountdownUI()
        {
            if (SelectionCountdownText != null)
                SelectionCountdownText.Text = _selectionCountdown.ToString();
                
            // Update progress ring
            if (CountdownProgress != null)
            {
                double percent = (double)_selectionCountdown / 7.0;
                CountdownProgress.StrokeDashOffset = FULL_DASH_ARRAY * (1 - percent);
                    }
                }

        private void NavigateToCapture()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _templates.Count) return;

            var selectedTemplate = _templates[_selectedIndex];
            var selectedMetadata = _templateMetadata[_selectedIndex];
            
            var captureWindow = new CaptureWindow(selectedTemplate, selectedMetadata);
            captureWindow.Show();
            this.Close();
        }


        private void LoadTemplates()
        {
            System.Diagnostics.Debug.WriteLine("=== LoadTemplates() in TemplateWindow ===");
            
            _templates.Clear();
            _templateDisplayImages.Clear();
            _templateNames.Clear();
            _templateMetadata.Clear();

            string assetsPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "templates");
            
            if (!Directory.Exists(assetsPath))
            {
                 Directory.CreateDirectory(assetsPath);
            }

            // Get all PNG files
            var pngFiles = Directory.GetFiles(assetsPath, "*.png");
            
            // Filter: exclude files ending with "double.png" (display images)
            // and ensure there is a corresponding .json metadata file
            var validTemplates = pngFiles
                .Where(f => !f.EndsWith(" double.png", StringComparison.OrdinalIgnoreCase))
                .Where(f => File.Exists(Path.ChangeExtension(f, ".json")))
                .OrderBy(f => Path.GetFileName(f)) // Sort alphabetically
                .ToArray();

            foreach (var filePath in validTemplates)
            {
                try
                {
                    var name = Path.GetFileName(filePath);
                    
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(filePath);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    _templates.Add(bi);
                    _templateNames.Add(name);
                    AddDisplayImage(name, assetsPath, bi);

                    // Load metadata JSON
                    var metadataName = Path.ChangeExtension(name, ".json");
                    var metadataPath = Path.Combine(assetsPath, metadataName);
                    var metadata = LoadTemplateMetadata(metadataPath);
                    _templateMetadata.Add(metadata);
                    
                    System.Diagnostics.Debug.WriteLine($"Loaded template: {name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR loading template {filePath}: {ex.Message}");
                }
            }
        }

        private void AddDisplayImage(string templateFileName, string assetsPath, BitmapImage fallbackImage)
        {
            // Check if there's a specific mapping or convention "Name double.png"
            // 1. Check mapping
            if (TemplatePreviewMap.TryGetValue(templateFileName, out var mappedName))
                        {
                 var previewPath = Path.Combine(assetsPath, mappedName);
                 var previewImage = TryLoadBitmap(previewPath);
                 if (previewImage != null)
                 {
                     _templateDisplayImages.Add(previewImage);
                     return;
                 }
            }

            // 2. Check convention: "Name.png" -> "Name double.png"
            var conventionName = Path.GetFileNameWithoutExtension(templateFileName) + " double.png";
            var conventionPath = Path.Combine(assetsPath, conventionName);
            var conventionImage = TryLoadBitmap(conventionPath);
            
            if (conventionImage != null)
            {
                _templateDisplayImages.Add(conventionImage);
            }
            else
            {
                // Fallback to original
                _templateDisplayImages.Add(fallbackImage);
            }
        }

        private static BitmapImage? TryLoadBitmap(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        private TemplateMetadata LoadTemplateMetadata(string metadataFilePath)
        {
            try
            {
                if (File.Exists(metadataFilePath))
                {
                    var json = File.ReadAllText(metadataFilePath);
                    var metadata = JsonSerializer.Deserialize<TemplateMetadata>(json);
                    if (metadata != null) return metadata;
                }
            }
            catch { }
            return new TemplateMetadata();
        }

        private void RenderTemplatePages()
        {
            TemplateStackPanel.Children.Clear();
            _templateContainers.Clear();

            int itemsPerPage = 6;
            int totalPages = (int)Math.Ceiling((double)_templates.Count / itemsPerPage);
            
            // Get the available width for the page content (Window Width - Margins)
            // Since Window is maximized, we can use SystemParameters or ActualWidth if loaded
            double pageWidth = this.ActualWidth > 0 ? this.ActualWidth : SystemParameters.PrimaryScreenWidth;
            // Adjust for margins defined in XAML (80 left + 80 right for ScrollViewer margin? No, let's check XAML)
            // XAML ScrollViewer Margin="80,0,80,0". So visible width is Width - 160.
            double visiblePageWidth = pageWidth - 160;

            for (int p = 0; p < totalPages; p++)
            {
                var pageGrid = new UniformGrid
                {
                    Rows = 2,
                    Columns = 3,
                    Width = visiblePageWidth,
                    Height = 580, // Reduced height to prevent clipping (was 650)
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                for (int i = 0; i < itemsPerPage; i++)
                {
                    int index = p * itemsPerPage + i;
                    if (index >= _templates.Count) break;

                    var container = CreateTemplateItem(index);
                    pageGrid.Children.Add(container);
                }

                // Add page to stack panel
                TemplateStackPanel.Children.Add(pageGrid);
            }
            
            UpdateScrollButtons();
        }

        private Grid CreateTemplateItem(int i)
            {
                var template = _templates[i];
            var displayImage = i < _templateDisplayImages.Count ? _templateDisplayImages[i] : template;
                var metadata = i < _templateMetadata.Count ? _templateMetadata[i] : null;
                
            // Create container grid
                var container = new Grid
                {
                    Tag = i,
                Margin = new Thickness(15), // Spacing between items
                Cursor = Cursors.Hand
                };

                // Create image
                var image = new Image
                {
                Source = displayImage,
                    Style = (Style)FindResource("TemplateImageStyle")
                };

            // --- Selection Indicators ---
                var boundary = metadata?.TemplateBoundary;
                double cornerRadius = boundary?.CornerRadius ?? 12;

            // Hidden elements that show on selection
            var selectionBorder = new Border
                {
                Name = "SelectionBorder",
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xEE)),
                BorderThickness = new Thickness(8), // Increased thickness (8 -> 9)
                    CornerRadius = new CornerRadius(cornerRadius),
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false
                };

                var checkmarkImage = new Image
                {
                    Name = "CheckmarkImage",
                    Source = new BitmapImage(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "red_check.png"))),
                    Width = 80,
                    Height = 80,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 10, 10)
                };

                container.Children.Add(image);
                container.Children.Add(selectionBorder);
                container.Children.Add(checkmarkImage);
            
                container.MouseDown += TemplateTile_Click;
                
            // Store index for positioning logic if needed (though UniformGrid handles layout)
                container.DataContext = i;

            // Handle SizeChanged for selection border scaling
                container.SizeChanged += (s, e) => {
                    if (s is Grid g && g.DataContext is int idx)
                    {
                        PositionSelectionBorders(g, idx);
                    }
                };

                _templateContainers.Add(container);
            return container;
        }

        private void TemplateTile_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid container || container.Tag is not int index)
                return;

            if (_selectedIndex == index) return;

            // Deselect previous
            if (_selectedIndex >= 0 && _selectedIndex < _templateContainers.Count)
            {
                var prevContainer = _templateContainers[_selectedIndex];
                foreach (var child in prevContainer.Children)
                {
                    if (child is Image img && img.Name == "CheckmarkImage") img.Visibility = Visibility.Collapsed;
                }
            }

            _selectedIndex = index;
            
            // Select new
            foreach (var child in container.Children)
            {
                if (child is Image img && img.Name == "CheckmarkImage")
                {
                    img.Visibility = Visibility.Visible;
                    // Animation
                    var scaleTransform = new ScaleTransform(0, 0, 40, 40);
                    img.RenderTransform = scaleTransform;
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new BackEase { Amplitude = 0.3 } };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                }
            }

            // Logic: If timer finished and was waiting for selection, go now!
            if (!_isCountdownRunning && _selectionCountdown <= 0)
            {
                NavigateToCapture();
            }
        }

        private void PositionSelectionBorders(Grid container, int index)
        {
            if (index < 0 || index >= _templateMetadata.Count)
                return;

            var meta = _templateMetadata[index];
            var bound = meta?.TemplateBoundary;
            if (bound == null || meta?.Resolution == null)
                return;

            // Calculate scale
            double scaleX = container.ActualWidth / meta.Resolution.Width;
            double scaleY = container.ActualHeight / meta.Resolution.Height;
            double scale = Math.Min(scaleX, scaleY);

            // Calculate centered offset for letterboxing
            double scaledWidth = meta.Resolution.Width * scale;
            double scaledHeight = meta.Resolution.Height * scale;
            double offsetX = (container.ActualWidth - scaledWidth) / 2;
            double offsetY = (container.ActualHeight - scaledHeight) / 2;

            // Position and size the overlay and border based on boundary
            foreach (var child in container.Children)
            {
                if (child is Border border && 
                    (border.Name == "SelectionOverlay" || border.Name == "SelectionBorder"))
                {
                    border.Width = bound.Width * scale;
                    border.Height = bound.Height * scale;
                    border.Margin = new Thickness(
                        offsetX + bound.X * scale,
                        offsetY + bound.Y * scale,
                        0, 0);
                    border.HorizontalAlignment = HorizontalAlignment.Left;
                    border.VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }
        
        // Removed LoadNextButtonImage and NextButton_Click as they are no longer used
        
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        // --- Scrolling Logic ---

        private void ScrollLeftButton_Click(object sender, RoutedEventArgs e)
        {
            // Scroll by one full page width
            double pageWidth = TemplateScrollViewer.ViewportWidth;
            TemplateScrollViewer.ScrollToHorizontalOffset(TemplateScrollViewer.HorizontalOffset - pageWidth);
            UpdateScrollButtons();
            }

        private void ScrollRightButton_Click(object sender, RoutedEventArgs e)
        {
            // Scroll by one full page width
            double pageWidth = TemplateScrollViewer.ViewportWidth;
            TemplateScrollViewer.ScrollToHorizontalOffset(TemplateScrollViewer.HorizontalOffset + pageWidth);
            UpdateScrollButtons();
        }

        private void TemplateScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Horizontal scroll with mouse wheel (Page Snap)
            double pageWidth = TemplateScrollViewer.ViewportWidth;
            if (e.Delta > 0)
                TemplateScrollViewer.ScrollToHorizontalOffset(TemplateScrollViewer.HorizontalOffset - pageWidth);
            else
                TemplateScrollViewer.ScrollToHorizontalOffset(TemplateScrollViewer.HorizontalOffset + pageWidth);
            
            e.Handled = true;
            UpdateScrollButtons();
        }
        
        private void UpdateScrollButtons()
        {
            // Simple visibility toggle (could be improved by checking scrollable width)
            // For now, just keep them visible if content might overflow
            // Or better, check actual offset on ScrollChanged event
        }

        // Added empty handler for collapsed Settings Button to prevent XAML errors
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
             // No action, button is hidden
        }
    }
}
