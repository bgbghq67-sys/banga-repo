using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Drawing.Printing;
using System.Management; // For WMI (Camera names)

namespace Banga_Photobooth
{
    public partial class SettingsWindow : Window
    {
        private string _displayImagePath;
        private string _useImagePath;
        private readonly string _templatesDir;

        public SettingsWindow()
        {
            InitializeComponent();
            _templatesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "templates");
            if (!Directory.Exists(_templatesDir)) Directory.CreateDirectory(_templatesDir);
            
            LoadTemplatesList();
            LoadConfig();
            LoadFonts();
            LoadAboutInfo();
            SetActiveTab("Templates");
        }

        private void LoadAboutInfo()
        {
            var libraries = new List<LibraryInfo>
            {
                new LibraryInfo { Name = "Microsoft .NET 8", License = "MIT License" },
                new LibraryInfo { Name = "OpenCvSharp4", License = "Apache License 2.0" },
                new LibraryInfo { Name = "OpenCvSharp4.WpfExtensions", License = "Apache License 2.0" },
                new LibraryInfo { Name = "OpenCvSharp4.runtime.win", License = "Apache License 2.0" },
                new LibraryInfo { Name = "Newtonsoft.Json", License = "MIT License" },
                new LibraryInfo { Name = "QRCoder", License = "MIT License" },
                new LibraryInfo { Name = "System.Drawing.Common", License = "MIT License" },
                new LibraryInfo { Name = "System.Management", License = "MIT License" }
            };
            LibrariesList.ItemsSource = libraries;
        }

        public class LibraryInfo
        {
            public string Name { get; set; }
            public string License { get; set; }
        }

        private void LoadFonts()
        {
            // Load system fonts
            var fonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            CboFonts.ItemsSource = fonts;

            // Set selected font
            string currentFont = BangaConfig.Current.SelectedFont;
            var selected = fonts.FirstOrDefault(f => f.Source.Equals(currentFont, StringComparison.OrdinalIgnoreCase));
            
            if (selected != null)
            {
                CboFonts.SelectedItem = selected;
            }
            else if (fonts.Count > 0)
            {
                // Try to find Poppins if valid, else default
                selected = fonts.FirstOrDefault(f => f.Source.Contains("Poppins")) ?? fonts.FirstOrDefault(f => f.Source.Contains("Segoe UI")) ?? fonts[0];
                CboFonts.SelectedItem = selected;
            }
        }

        private void CboFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboFonts.SelectedItem is FontFamily font)
            {
                BangaConfig.Current.SelectedFont = font.Source;
                BangaConfig.Save();

                // Update Preview
                TxtFontPreview.FontFamily = font;
                TxtFontPreview2.FontFamily = font;
            }
        }

        private void LoadConfig()
        {
            // Initialize Config UI
            TglCameraSimulation.IsChecked = BangaConfig.Current.CameraSimulationMode;
            TglPrinterSimulation.IsChecked = BangaConfig.Current.PrinterSimulationMode;
            TglInvertCamera.IsChecked = BangaConfig.Current.InvertCamera;
            
            TxtCountdown.Text = BangaConfig.Current.CountdownSeconds.ToString();

            // Load Lists
            LoadWebcams();
            LoadPrinters();
            
            // Update Visibility based on state
            UpdateCameraUI();
            UpdatePrinterUI();
        }

        private void LoadWebcams()
        {
            CboWebcams.Items.Clear();
            var cameras = new List<string>();
            
            // Try to fetch real camera names using WMI (System.Management)
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
                {
                    foreach (var device in searcher.Get())
                    {
                        string name = device["Caption"]?.ToString() ?? device["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            cameras.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WMI Error: {ex.Message}");
            }

            // Fallback or add generic indices if WMI failed or returned nothing
            // Note: WMI names don't map 1:1 to OpenCV indices perfectly in all cases,
            // but typically they appear in order.
            if (cameras.Count == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    cameras.Add($"Camera Index {i}");
                }
            }
            else
            {
                // Add indices to names to be helpful
                for (int i = 0; i < cameras.Count; i++)
                {
                    cameras[i] = $"{cameras[i]} (Index {i})";
                }
                
                // Append extra generic slots just in case WMI missed something active
                for (int i = cameras.Count; i < 5; i++)
                {
                    cameras.Add($"Camera Index {i}");
                }
            }

            foreach (var cam in cameras)
            {
                CboWebcams.Items.Add(cam);
            }
            
            // Select current
            if (BangaConfig.Current.SelectedWebcamIndex >= 0 && BangaConfig.Current.SelectedWebcamIndex < CboWebcams.Items.Count)
            {
                CboWebcams.SelectedIndex = BangaConfig.Current.SelectedWebcamIndex;
            }
            else
            {
                CboWebcams.SelectedIndex = 0;
            }
        }

        private void LoadPrinters()
        {
            try
            {
                CboPrinterStrip.Items.Clear();
                CboPrinter4R.Items.Clear();
                
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    CboPrinterStrip.Items.Add(printer);
                    CboPrinter4R.Items.Add(printer);
                }

                // Load Strip Printer selection
                if (!string.IsNullOrEmpty(BangaConfig.Current.SelectedPrinterStrip) && CboPrinterStrip.Items.Contains(BangaConfig.Current.SelectedPrinterStrip))
                {
                    CboPrinterStrip.SelectedItem = BangaConfig.Current.SelectedPrinterStrip;
                }
                else if (!string.IsNullOrEmpty(BangaConfig.Current.SelectedPrinter) && CboPrinterStrip.Items.Contains(BangaConfig.Current.SelectedPrinter))
                {
                    // Fallback to legacy single printer
                    CboPrinterStrip.SelectedItem = BangaConfig.Current.SelectedPrinter;
                }
                else if (CboPrinterStrip.Items.Count > 0)
                {
                    CboPrinterStrip.SelectedIndex = 0;
                }

                // Load 4R Printer selection
                if (!string.IsNullOrEmpty(BangaConfig.Current.SelectedPrinter4R) && CboPrinter4R.Items.Contains(BangaConfig.Current.SelectedPrinter4R))
                {
                    CboPrinter4R.SelectedItem = BangaConfig.Current.SelectedPrinter4R;
                }
                else if (!string.IsNullOrEmpty(BangaConfig.Current.SelectedPrinter) && CboPrinter4R.Items.Contains(BangaConfig.Current.SelectedPrinter))
                {
                    // Fallback to legacy single printer
                    CboPrinter4R.SelectedItem = BangaConfig.Current.SelectedPrinter;
                }
                else if (CboPrinter4R.Items.Count > 0)
                {
                    CboPrinter4R.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading printers: {ex.Message}");
            }
        }
        
        private void UpdateCameraUI()
        {
             bool isSim = TglCameraSimulation.IsChecked == true;
             WebcamPanel.Visibility = isSim ? Visibility.Visible : Visibility.Collapsed;
             CanonPanel.Visibility = isSim ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdatePrinterUI()
        {
             bool isSim = TglPrinterSimulation.IsChecked == true;
             PrinterPanel.Visibility = isSim ? Visibility.Collapsed : Visibility.Visible;
             FileSavePanel.Visibility = isSim ? Visibility.Visible : Visibility.Collapsed;
        }

        // --- Events ---

        private void TglCameraSimulation_Checked(object sender, RoutedEventArgs e)
        {
            BangaConfig.Current.CameraSimulationMode = TglCameraSimulation.IsChecked == true;
            BangaConfig.Save();
            UpdateCameraUI();
        }

        private void TglInvertCamera_Changed(object sender, RoutedEventArgs e)
        {
            BangaConfig.Current.InvertCamera = TglInvertCamera.IsChecked == true;
            BangaConfig.Save();
        }

        private void TglPrinterSimulation_Checked(object sender, RoutedEventArgs e)
        {
            BangaConfig.Current.PrinterSimulationMode = TglPrinterSimulation.IsChecked == true;
            BangaConfig.Save();
            UpdatePrinterUI();
        }

        private void CboWebcams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             if (CboWebcams.SelectedIndex >= 0)
             {
                 BangaConfig.Current.SelectedWebcamIndex = CboWebcams.SelectedIndex;
                 BangaConfig.Save();
             }
        }
        
        private void CboPrinterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboPrinterStrip.SelectedItem is string printerName)
            {
                BangaConfig.Current.SelectedPrinterStrip = printerName;
                BangaConfig.Current.SelectedPrinter = printerName; // Keep legacy updated
                BangaConfig.Save();
            }
        }
        
        private void CboPrinter4R_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboPrinter4R.SelectedItem is string printerName)
            {
                BangaConfig.Current.SelectedPrinter4R = printerName;
                BangaConfig.Save();
            }
        }
        
        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
             string msg = 
                 "BANGA PHOTOBOOTH - PLUG & PLAY SETUP GUIDE\n\n" +
                 "1. CAMERA (Canon EOS 2000D)\n" +
                 "   - Connect via USB cable.\n" +
                 "   - Turn camera ON.\n" +
                 "   - Set Mode Dial to 'Movie Mode' (Video Icon) or 'M' (Manual).\n" +
                 "   - Important: Close any other Canon Utility software.\n" +
                 "   - No separate driver installation needed (Uses Windows Drivers).\n\n" +
                 "2. PRINTER (DNP DS-RX1HS)\n" +
                 "   - Connect via USB and Power ON.\n" +
                 "   - Install Driver: Run 'DriverInstall.CMD' in Drivers folder.\n" +
                 "   - Settings: Set Paper Size to '4x6'.\n\n" +
                 "TROUBLESHOOTING:\n" +
                 "- If camera not found, unplug/replug USB and restart App.\n" +
                 "- If printer not found, check Windows 'Printers & Scanners'.";
                 
             MessageBox.Show(msg, "Setup Guide", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // --- Existing Template Logic & Standard Boilerplate ---

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                SetActiveTab(tag);
            }
        }

        private void SetActiveTab(string tag)
        {
            // Reset Styles
            BtnTemplates.Style = (Style)FindResource("SidebarButtonStyle");
            BtnConfig.Style = (Style)FindResource("SidebarButtonStyle");
            BtnAppearance.Style = (Style)FindResource("SidebarButtonStyle");
            BtnAbout.Style = (Style)FindResource("SidebarButtonStyle");

            // Hide Pages
            TemplatesPage.Visibility = Visibility.Collapsed;
            ConfigPage.Visibility = Visibility.Collapsed;
            PlaceholderPage.Visibility = Visibility.Collapsed;

            // Activate Logic
            switch (tag)
            {
                case "Templates":
                    BtnTemplates.Style = (Style)FindResource("ActiveSidebarButtonStyle");
                    TemplatesPage.Visibility = Visibility.Visible;
                    break;
                case "Config":
                    BtnConfig.Style = (Style)FindResource("ActiveSidebarButtonStyle");
                    ConfigPage.Visibility = Visibility.Visible;
                    break;
                case "Appearance":
                    BtnAppearance.Style = (Style)FindResource("ActiveSidebarButtonStyle");
                    AppearancePage.Visibility = Visibility.Visible;
                    break;
                case "About":
                    BtnAbout.Style = (Style)FindResource("ActiveSidebarButtonStyle");
                    AboutPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class TemplateListItem
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
        }

        private void LoadTemplatesList()
        {
            try
            {
                var pngFiles = Directory.GetFiles(_templatesDir, "*.png");
                var templates = pngFiles
                    .Where(f => !f.EndsWith(" double.png", StringComparison.OrdinalIgnoreCase))
                    .Where(f => File.Exists(Path.ChangeExtension(f, ".json")))
                    .Select(f => new TemplateListItem { Name = Path.GetFileNameWithoutExtension(f), FullPath = f })
                    .OrderBy(t => t.Name)
                    .ToList();

                TemplatesListBox.ItemsSource = templates;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading templates: {ex.Message}");
            }
        }
        
        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            BtnClear_Click(sender, e);
            TxtEditorTitle.Text = "New Template";
        }

        private void TemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplatesListBox.SelectedItem is TemplateListItem item)
            {
                LoadTemplateToForm(item);
                TxtEditorTitle.Text = $"Edit: {item.Name}";
            }
        }

        private void LoadTemplateToForm(TemplateListItem item)
        {
            TxtTemplateName.Text = item.Name;
            
            // Load Images
            string displayPath = Path.Combine(_templatesDir, item.Name + " double.png");
            string usePath = item.FullPath;

            // If specific display image doesn't exist, fallback logic (usually handled in TemplateWindow, but here we try to find exact match)
            if (!File.Exists(displayPath)) displayPath = usePath; // Fallback for preview

            if (File.Exists(displayPath))
            {
                _displayImagePath = displayPath; // Keep track for logic, though we might not move it if not changed
                ImgDisplayPreview.Source = new BitmapImage(new Uri(displayPath));
            }

            if (File.Exists(usePath))
            {
                _useImagePath = usePath;
                ImgUsePreview.Source = new BitmapImage(new Uri(usePath));
            }

            // Load Metadata
            string jsonPath = Path.ChangeExtension(item.FullPath, ".json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var metadata = JsonSerializer.Deserialize<TemplateMetadata>(json);
                    
                    if (metadata != null)
                    {
                        TxtWidth.Text = metadata.Resolution?.Width.ToString() ?? "0";
                        TxtHeight.Text = metadata.Resolution?.Height.ToString() ?? "0";
                        
                        TxtPhotoSlots.Text = JsonSerializer.Serialize(metadata.PhotoSlots, new JsonSerializerOptions { WriteIndented = true });
                        TxtQrSlot.Text = JsonSerializer.Serialize(metadata.QrSlot);
                        TxtBoundary.Text = JsonSerializer.Serialize(metadata.TemplateBoundary);
                    }
                }
                catch { }
            }
        }

        private void BtnSelectDisplayImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                _displayImagePath = dialog.FileName;
                ImgDisplayPreview.Source = new BitmapImage(new Uri(_displayImagePath));
            }
        }

        private void BtnSelectUseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                _useImagePath = dialog.FileName;
                ImgUsePreview.Source = new BitmapImage(new Uri(_useImagePath));
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtTemplateName.Text = "";
            ImgDisplayPreview.Source = null;
            ImgUsePreview.Source = null;
            TxtWidth.Text = "";
            TxtHeight.Text = "";
            TxtPhotoSlots.Text = "";
            TxtQrSlot.Text = "";
            TxtBoundary.Text = "";
            
            _displayImagePath = null;
            _useImagePath = null;
            TemplatesListBox.SelectedIndex = -1;
            TxtEditorTitle.Text = "New Template";
        }

        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtTemplateName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a template name.");
                return;
            }

            if (string.IsNullOrEmpty(_useImagePath) || string.IsNullOrEmpty(_displayImagePath))
            {
                MessageBox.Show("Please select both Display and Use images.");
                return;
            }

            try
            {
                // 1. Save Images
                string destUse = Path.Combine(_templatesDir, name + ".png");
                string destDisplay = Path.Combine(_templatesDir, name + " double.png");

                // Copy if source is different from dest (avoid locking issues or overwriting same file)
                if (_useImagePath != destUse)
                    File.Copy(_useImagePath, destUse, true);
                
                if (_displayImagePath != destDisplay)
                    File.Copy(_displayImagePath, destDisplay, true);

                // 2. Construct Metadata
                var metadata = new TemplateMetadata
                {
                    TemplateName = name,
                    Resolution = new Resolution 
                    { 
                        Width = (int)double.Parse(TxtWidth.Text), 
                        Height = (int)double.Parse(TxtHeight.Text) 
                    },
                    PhotoSlots = JsonSerializer.Deserialize<List<PhotoSlot>>(TxtPhotoSlots.Text),
                    QrSlot = JsonSerializer.Deserialize<Slot>(TxtQrSlot.Text),
                    TemplateBoundary = JsonSerializer.Deserialize<TemplateBoundary>(TxtBoundary.Text)
                };

                string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(_templatesDir, name + ".json"), json);

                MessageBox.Show("Template Saved Successfully!");
                LoadTemplatesList(); // Refresh list
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}");
            }
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !double.TryParse(e.Text, out _);
        }

        private void TxtCountdown_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TxtCountdown.Text, out int val))
            {
                BangaConfig.Current.CountdownSeconds = val;
                BangaConfig.Save();
            }
        }
        
        private void BtnCheckPrinter_Click(object sender, RoutedEventArgs e)
        {
            // Refresh printer list
            LoadPrinters();
            
            // Check both printers
            string stripPrinter = CboPrinterStrip.SelectedItem as string;
            string printer4R = CboPrinter4R.SelectedItem as string;
            
            bool stripOk = false;
            bool fourROk = false;
            
            try 
            {
                if (!string.IsNullOrEmpty(stripPrinter))
                {
                    var ps = new PrinterSettings { PrinterName = stripPrinter };
                    stripOk = ps.IsValid;
                }
                
                if (!string.IsNullOrEmpty(printer4R))
                {
                    var ps = new PrinterSettings { PrinterName = printer4R };
                    fourROk = ps.IsValid;
                }
                
                if (stripOk && fourROk)
                {
                    TxtPrinterStatus.Text = "Status: Both printers ready ✓";
                    TxtPrinterStatus.Foreground = Brushes.Green;
                }
                else if (stripOk || fourROk)
                {
                    TxtPrinterStatus.Text = $"Status: Strip={stripOk}, 4R={fourROk}";
                    TxtPrinterStatus.Foreground = Brushes.Orange;
                }
                else
                {
                    TxtPrinterStatus.Text = "Status: No valid printers";
                    TxtPrinterStatus.Foreground = Brushes.Red;
                }
            }
            catch 
            { 
                TxtPrinterStatus.Text = "Status: Error checking printers"; 
                TxtPrinterStatus.Foreground = Brushes.Red; 
            }
        }
    }
}
