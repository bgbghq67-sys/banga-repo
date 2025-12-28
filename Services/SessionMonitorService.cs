using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Banga_Photobooth.Services
{
    public class SessionMonitorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SessionMonitorService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _baseUrl = BangaConfig.Current.ApiBaseUrl?.TrimEnd('/') ?? "https://banga-photobooth-admin.vercel.app";
        }

        /// <summary>
        /// Get remaining sessions for this device
        /// </summary>
        public async Task<int> GetRemainingSessionsAsync()
        {
            try
            {
                string machineId = MachineIdService.GetMachineId();
                
                var payload = new { machineId = machineId };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/devices/status", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<StatusResponse>(responseJson);
                    return result?.remainingSessions ?? 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching session status: " + ex.Message);
            }
            return 0;
        }

        /// <summary>
        /// Decrement session count for this device after print
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
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("Session decremented successfully");
                    return true;
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Decrement failed: {errorJson}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error decrementing session: " + ex.Message);
            }
            return false;
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
}
