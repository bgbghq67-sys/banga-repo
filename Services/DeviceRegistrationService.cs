using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Banga_Photobooth.Services
{
    public class DeviceRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public DeviceRegistrationService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // Use the same base URL as other services
            _baseUrl = BangaConfig.Current.ApiBaseUrl?.TrimEnd('/') ?? "https://banga-photobooth-admin.vercel.app";
        }

        /// <summary>
        /// Register this device with the server. Creates new device if not exists.
        /// </summary>
        public async Task<DeviceStatusResponse> RegisterDeviceAsync()
        {
            try
            {
                string machineId = MachineIdService.GetMachineId();
                string machineName = Environment.MachineName;

                var payload = new
                {
                    machineId = machineId,
                    machineName = machineName
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/register", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<RegisterResponse>(responseJson);
                    return new DeviceStatusResponse
                    {
                        Success = true,
                        DeviceId = result?.deviceId ?? "",
                        DeviceName = result?.deviceName ?? "",
                        RemainingSessions = result?.remainingSessions ?? 0,
                        IsActivated = result?.activated ?? false,
                        IsNewDevice = result?.isNew ?? false
                    };
                }

                return new DeviceStatusResponse { Success = false, ErrorMessage = "Registration failed" };
            }
            catch (Exception ex)
            {
                return new DeviceStatusResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Check current device status (session count, activation status)
        /// </summary>
        public async Task<DeviceStatusResponse> CheckStatusAsync()
        {
            try
            {
                string machineId = MachineIdService.GetMachineId();

                var payload = new { machineId = machineId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/status", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StatusResponse>(responseJson);
                    return new DeviceStatusResponse
                    {
                        Success = true,
                        DeviceId = result?.deviceId ?? "",
                        DeviceName = result?.deviceName ?? "",
                        RemainingSessions = result?.remainingSessions ?? 0,
                        IsActivated = result?.activated ?? false
                    };
                }

                return new DeviceStatusResponse { Success = false, RemainingSessions = 0 };
            }
            catch (Exception ex)
            {
                return new DeviceStatusResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Decrement session count after a print
        /// </summary>
        public async Task<bool> DecrementSessionAsync()
        {
            try
            {
                string machineId = MachineIdService.GetMachineId();

                var payload = new { machineId = machineId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/decrement", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // JSON response classes
        private class RegisterResponse
        {
            public bool ok { get; set; }
            public bool isNew { get; set; }
            public string? deviceId { get; set; }
            public string? deviceName { get; set; }
            public int remainingSessions { get; set; }
            public bool activated { get; set; }
        }

        private class StatusResponse
        {
            public bool ok { get; set; }
            public string? deviceId { get; set; }
            public string? deviceName { get; set; }
            public int remainingSessions { get; set; }
            public bool activated { get; set; }
        }
    }

    public class DeviceStatusResponse
    {
        public bool Success { get; set; }
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public int RemainingSessions { get; set; }
        public bool IsActivated { get; set; }
        public bool IsNewDevice { get; set; }
        public string? ErrorMessage { get; set; }
    }
}








