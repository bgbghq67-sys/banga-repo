using System;
using System.IO;
using System.Text.Json;

namespace Banga_Photobooth
{
    public class AppConfig
    {
        // Split Simulation Modes
        public bool CameraSimulationMode { get; set; } = true; // True = Webcam, False = Canon
        public bool PrinterSimulationMode { get; set; } = true; // True = Save File, False = Physical Print
        
        public int SelectedWebcamIndex { get; set; } = 0;
        
        // Legacy single printer (for backwards compatibility)
        public string SelectedPrinter { get; set; } = string.Empty;
        
        // NEW: Two printer profiles for different modes
        public string SelectedPrinterStrip { get; set; } = string.Empty; // For 600x1800 Strip mode (Cut ON)
        public string SelectedPrinter4R { get; set; } = string.Empty;    // For 1200x1800 4R mode (Cut OFF)
        
        public int CountdownSeconds { get; set; } = 5;
        public string SelectedFont { get; set; } = "Poppins"; // Default Font
        public string ApiBaseUrl { get; set; } = "https://banga-photobooth-admin.vercel.app";
        public bool InvertCamera { get; set; } = false; // Mirror/Flip camera horizontally
    }

    public static class BangaConfig
    {
        private static string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public static AppConfig Current { get; private set; }

        static BangaConfig()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    Current = new AppConfig();
                }
            }
            catch
            {
                Current = new AppConfig();
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}
