using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Input;

namespace Banga_Photobooth
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SoundPlayer _clickSound;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize Sound Player
            try
            {
                string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                // Look specifically for BUBBLE.wav first
                string soundPath = Path.Combine(assetsPath, "BUBBLE.wav");
                
                // If BUBBLE.wav isn't there, try lowercase bubble.wav
                if (!File.Exists(soundPath))
                {
                     string lowerCasePath = Path.Combine(assetsPath, "bubble.wav");
                     if (File.Exists(lowerCasePath))
                     {
                         soundPath = lowerCasePath;
                     }
                     else
                     {
                         // Fallback: Try to find ANY wav file
                         var wavFiles = Directory.GetFiles(assetsPath, "*.wav");
                         if (wavFiles.Length > 0)
                         {
                             soundPath = wavFiles[0];
                         }
                     }
                }

                if (File.Exists(soundPath))
                {
                    _clickSound = new SoundPlayer(soundPath);
                    _clickSound.Load(); // Preload
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sound: {ex.Message}");
            }

            // Register Global Event Handler for Mouse/Touch
            EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnGlobalMouseDown));
            EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewTouchDownEvent, new EventHandler<TouchEventArgs>(OnGlobalTouchDown));
        }

        private void OnGlobalMouseDown(object sender, MouseButtonEventArgs e)
        {
            PlayClickSound();
        }

        private void OnGlobalTouchDown(object sender, TouchEventArgs e)
        {
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            if (_clickSound != null)
            {
                try
                {
                    // Play uses a new thread, so it won't block UI
                    _clickSound.Play();
                }
                catch
                {
                    // Ignore playback errors
                }
            }
        }
    }
}
