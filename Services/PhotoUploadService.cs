using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Banga_Photobooth.Services
{
    public class SessionResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class PhotoUploadService
    {
        private readonly HttpClient _httpClient;

        public PhotoUploadService()
        {
            _httpClient = new HttpClient();
            // Use configured URL or fallback
            string baseUrl = BangaConfig.Current.ApiBaseUrl;
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://banga-photobooth-admin.vercel.app";
            
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<SessionResponse?> UploadSessionAsync(string originalPhotoPath, string aiPhotoPath, string zipPath = null)
        {
            try
            {
                // Use HttpWebRequest for manual multipart construction to guarantee compatibility with strict parsers
                // Undici (Node 18+) is extremely strict about quotes and CRLF
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                
                // Prepare request
                var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(_httpClient.BaseAddress + "api/session");
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;
                request.KeepAlive = true;

                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    // Helper to write string
                    async Task WriteStringAsync(Stream stream, string text)
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }

                    // Helper to write file
                    async Task WriteFileAsync(string path, string fieldName)
                    {
                        if (File.Exists(path))
                        {
                            string fileName = Path.GetFileName(path);
                            // Format: --boundary\r\nContent-Disposition: form-data; name="fieldName"; filename="fileName"\r\nContent-Type: image/png\r\n\r\n
                            string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{fieldName}\"; filename=\"{fileName}\"\r\nContent-Type: image/png\r\n\r\n";
                            
                            await WriteStringAsync(requestStream, header);
                            
                            using (var fileStream = File.OpenRead(path))
                            {
                                await fileStream.CopyToAsync(requestStream);
                            }
                            
                            await WriteStringAsync(requestStream, "\r\n");
                        }
                    }

                    // Write Original
                    await WriteFileAsync(originalPhotoPath, "photo_original");

                    // Write AI
                    await WriteFileAsync(aiPhotoPath, "photo_ai");

                    // End boundary
                    await WriteStringAsync(requestStream, $"--{boundary}--\r\n");
                }

                using (var response = (System.Net.HttpWebResponse)await request.GetResponseAsync())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseString = await streamReader.ReadToEndAsync();
                    return JsonSerializer.Deserialize<SessionResponse>(responseString);
                }
            }
            catch (System.Net.WebException webEx)
            {
                if (webEx.Response != null)
                {
                    using (var errorStream = webEx.Response.GetResponseStream())
                    using (var reader = new StreamReader(errorStream))
                    {
                        string errorText = await reader.ReadToEndAsync();
                        System.Diagnostics.Debug.WriteLine($"WebException: {errorText}");
                        return new SessionResponse { Ok = false, Message = $"HTTP Error: {errorText}" };
                    }
                }
                return new SessionResponse { Ok = false, Message = $"WebException: {webEx.Message}" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload exception: {ex.Message}");
                return new SessionResponse { Ok = false, Message = $"Exception: {ex.Message}" };
            }
        }
    }
}

