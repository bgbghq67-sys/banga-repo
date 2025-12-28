using System;
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

        public OtpWindow()
        {
            InitializeComponent();
            _deviceService = new DeviceRegistrationService();
            
            // Show Machine ID
            string machineId = MachineIdService.GetMachineId();
            MachineIdText.Text = FormatMachineId(machineId);

            // Poll every 2 seconds
            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(2);
            _pollTimer.Tick += PollTimer_Tick;

            // Register device on load
            Loaded += OtpWindow_Loaded;
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
            UpdateStatus("Registering device...", "#3498DB");

            var result = await _deviceService.RegisterDeviceAsync();

            if (result.Success)
            {
                _isRegistered = true;

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
                    UpdateStatus("Waiting for activation...", "#3498DB");
                }

                // Start polling
                _pollTimer.Start();
            }
            else
            {
                UpdateStatus($"Connection error. Retrying...", "#E74C3C");
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
