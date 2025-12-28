using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Banga_Photobooth.Services
{
    public class OtpService
    {
        private HttpClient _httpClient;

        public OtpService()
        {
            _httpClient = new HttpClient();
        }

        private string GetBaseUrl()
        {
            string baseUrl = BangaConfig.Current.ApiBaseUrl;
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://banga-photobooth-admin.vercel.app";
            return $"{baseUrl.TrimEnd('/')}/api/otp";
        }

        public async Task<bool> VerifyOtpAsync(string code)
        {
            try
            {
                var payload = new { action = "verify", code = code };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GetBaseUrl(), content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseString);
                    return result.ok == true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OTP Verification Error: {ex.Message}");
                return false;
            }
        }
    }
}

