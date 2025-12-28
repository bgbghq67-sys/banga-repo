using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Banga_Photobooth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load images from file paths (for single-file publish)
            var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            
            // Set background image
            var bgPath = System.IO.Path.Combine(assetsPath, "welcomebg.png");
            if (System.IO.File.Exists(bgPath))
            {
                var bgBrush = new ImageBrush(new BitmapImage(new Uri(bgPath)));
                bgBrush.Stretch = Stretch.UniformToFill;
                ((Grid)Content).Background = bgBrush;
            }
            
            // Set logo image
            var logoPath = System.IO.Path.Combine(assetsPath, "Logo.png");
            if (System.IO.File.Exists(logoPath))
            {
                LogoImage.Source = new BitmapImage(new Uri(logoPath));
            }
        }

        // Secret gesture variables
        private int _secretClickCount = 0;
        private const int SECRET_CLICK_THRESHOLD = 5;
        private System.Windows.Threading.DispatcherTimer _clickTimer;

        private void LogoImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent Grid_MouseDown from firing immediately

            // Initialize timer if not already done
            if (_clickTimer == null)
            {
                _clickTimer = new System.Windows.Threading.DispatcherTimer();
                _clickTimer.Interval = TimeSpan.FromMilliseconds(500); // 500ms window between clicks
                _clickTimer.Tick += ClickTimer_Tick;
            }

            // Reset the timer on every click
            _clickTimer.Stop();
            _secretClickCount++;
            System.Diagnostics.Debug.WriteLine($"Secret Click: {_secretClickCount}");

            if (_secretClickCount >= SECRET_CLICK_THRESHOLD)
            {
                // Threshold reached! Open Settings
                _secretClickCount = 0;
                _clickTimer.Stop(); // Ensure timer doesn't fire

                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            else
            {
                // Wait for more clicks or timeout
                _clickTimer.Start();
            }
        }

        private void ClickTimer_Tick(object sender, EventArgs e)
        {
            _clickTimer.Stop();
            
            // Timer finished, meaning user stopped clicking.
            // If we didn't reach the threshold, treat it as a normal "Start" intent.
            if (_secretClickCount > 0 && _secretClickCount < SECRET_CLICK_THRESHOLD)
            {
                _secretClickCount = 0;
                StartApp();
            }
        }


        private void StartApp()
        {
             var templateWindow = new TemplateWindow();
             templateWindow.Show();
             this.Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartApp();
        }
    }
}