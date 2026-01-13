using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Banga_Photobooth.Services;

namespace Banga_Photobooth
{
    public partial class OtpWindow : Window
    {
        private readonly DeviceRegistrationService _deviceService;
        private readonly DispatcherTimer _pollTimer;
        private bool _isUnlocking = false;
        private bool _isRegistered = false;
        private int _retryCount = 0;
        private const int MAX_RETRIES = 10; // Maximum retry attempts before showing manual retry button

        public OtpWindow()
        {
            InitializeComponent();
            _deviceService = new DeviceRegistrationService();
            
            // Show Machine ID
            string machineId = MachineIdService.GetMachineId();
            MachineIdText.Text = FormatMachineId(machineId);

            // Poll every 3 seconds (increased from 2)
            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(3);
            _pollTimer.Tick += PollTimer_Tick;

            // Register device on load
            Loaded += OtpWindow_Loaded;
        }
        
        private void LogError(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connection_log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                File.AppendAllText(logPath, logEntry);
            }
            catch { }
        }

        private string FormatMachineId(string id)
        {
            // Format as XXXX-XXXX-XXXX-XXXX for readability
            if (id.Length >= 16)
            {
                return $"{id.Substring(0, 4)}-{id.Substring(4, 4)}-{id.Substring(8, 4)}-{id.Substring(12, 4)}";
            }
            return id;
        }

        private async void OtpWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RegisterDevice();
        }

        private async System.Threading.Tasks.Task RegisterDevice()
        {
            _retryCount++;
            UpdateStatus($"Connecting to server... (Attempt {_retryCount})", "#3498DB");
            LogError($"Registration attempt {_retryCount} started");

            var result = await _deviceService.RegisterDeviceAsync();

            if (result.Success)
            {
                _isRegistered = true;
                _retryCount = 0; // Reset retry count on success
                LogError("Registration successful");

                if (result.IsNewDevice)
                {
                    UpdateStatus("Device registered! Waiting for activation...", "#F39C12");
                }
                else if (result.IsActivated)
                {
                    UpdateStatus("Device activated!", "#27AE60");
                    UnlockApp();
                    return;
                }
                else
                {
                    UpdateStatus("Waiting for activation from admin...", "#3498DB");
                }

                // Start polling
                _pollTimer.Start();
            }
            else
            {
                string errorMsg = result.ErrorMessage ?? "Unknown error";
                LogError($"Registration failed: {errorMsg}");
                
                if (_retryCount >= MAX_RETRIES)
                {
                    // Show detailed error and stop auto-retry
                    UpdateStatus($"Connection failed. Check internet.", "#E74C3C");
                    LogError($"Max retries ({MAX_RETRIES}) reached. Stopping auto-retry.");
                    
                    // Show retry button or instruction
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Cannot connect to server after {MAX_RETRIES} attempts.\n\n" +
                            $"Error: {errorMsg}\n\n" +
                            "Please check:\n" +
                            "1. Internet connection\n" +
                            "2. Firewall settings\n" +
                            "3. Try restarting the app\n\n" +
                            "The app will try again when you click OK.",
                            "Connection Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        
                        // Reset and try again
                        _retryCount = 0;
                    });
                }
                else
                {
                    UpdateStatus($"Connection error. Retry {_retryCount}/{MAX_RETRIES}...", "#E74C3C");
                }
                
                // Retry registration after 5 seconds
                await System.Threading.Tasks.Task.Delay(5000);
                await RegisterDevice();
            }
        }

        private async void PollTimer_Tick(object? sender, EventArgs e)
        {
            if (_isUnlocking || !_isRegistered) return;

            var result = await _deviceService.CheckStatusAsync();

            if (result.Success && result.IsActivated)
            {
                UpdateStatus("Activated! Starting...", "#27AE60");
                UnlockApp();
            }
        }

        private void UpdateStatus(string message, string colorHex)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                StatusText.Foreground = new SolidColorBrush(color);
                StatusDot.Fill = new SolidColorBrush(color);
            });
        }

        private void UnlockApp()
        {
            if (_isUnlocking) return;
            _isUnlocking = true;
            _pollTimer.Stop();

            Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            });
        }
    }
}
