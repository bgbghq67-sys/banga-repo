using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace Banga_Photobooth.Services
{
    public static class MachineIdService
    {
        private static string? _cachedMachineId;
        private static readonly string MachineIdFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BangaPhotobooth",
            "machine_id.txt"
        );

        /// <summary>
        /// Gets a unique, persistent Machine ID for this computer.
        /// Uses hardware IDs (CPU + Motherboard) hashed together.
        /// Falls back to a generated GUID if hardware IDs are unavailable.
        /// </summary>
        public static string GetMachineId()
        {
            if (!string.IsNullOrEmpty(_cachedMachineId))
                return _cachedMachineId;

            // Try to read from cached file first
            try
            {
                if (File.Exists(MachineIdFile))
                {
                    _cachedMachineId = File.ReadAllText(MachineIdFile).Trim();
                    if (!string.IsNullOrEmpty(_cachedMachineId))
                        return _cachedMachineId;
                }
            }
            catch { }

            // Generate new Machine ID
            _cachedMachineId = GenerateMachineId();

            // Save to file for persistence
            try
            {
                var dir = Path.GetDirectoryName(MachineIdFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(MachineIdFile, _cachedMachineId);
            }
            catch { }

            return _cachedMachineId;
        }

        private static string GenerateMachineId()
        {
            try
            {
                string cpuId = GetCpuId();
                string motherboardId = GetMotherboardId();
                string combined = $"{cpuId}-{motherboardId}";

                // Hash the combined IDs
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    // Take first 16 bytes and convert to hex
                    return BitConverter.ToString(hashBytes, 0, 16).Replace("-", "").ToUpperInvariant();
                }
            }
            catch
            {
                // Fallback to GUID if hardware ID retrieval fails
                return Guid.NewGuid().ToString("N").ToUpperInvariant();
            }
        }

        private static string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["ProcessorId"]?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "UNKNOWN_CPU";
        }

        private static string GetMotherboardId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "";
                    }
                }
            }
            catch { }
            return "UNKNOWN_MB";
        }
    }
}








