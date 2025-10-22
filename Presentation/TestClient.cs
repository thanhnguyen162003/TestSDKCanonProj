using Presentation.Models.ResponseModels;
using System.Text.Json;

namespace Presentation
{
    public class TestClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HttpClient HttpClient => _httpClient;
        public string BaseUrl => _baseUrl;

        public TestClient(string baseUrl = "https://localhost:7000")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            
            // Ignore SSL certificate errors for local testing
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Canon SDK Test Client");
        }

        /// <summary>
        /// Get list of connected cameras
        /// </summary>
        public async Task<List<CameraInfo>> GetCamerasAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/cameras");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var cameras = JsonSerializer.Deserialize<List<CameraInfo>>(json);
            return cameras ?? new List<CameraInfo>();
        }

        /// <summary>
        /// Open session with a camera
        /// </summary>
        public async Task<bool> OpenSessionAsync(string cameraRef)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/cameras/{cameraRef}/session", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Close camera session
        /// </summary>
        public async Task<bool> CloseSessionAsync()
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/cameras/session");
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get camera status
        /// </summary>
        public async Task<CameraStatus> GetStatusAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/cameras/status");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CameraStatus>(json) ?? new CameraStatus();
        }

        /// <summary>
        /// Take a photo
        /// </summary>
        public async Task<bool> TakePhotoAsync()
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/cameras/photo", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Start live view
        /// </summary>
        public async Task<bool> StartLiveViewAsync()
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/cameras/liveview/start", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Stop live view
        /// </summary>
        public async Task<bool> StopLiveViewAsync(bool lvOff = true)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/cameras/liveview/stop?lvOff={lvOff}", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get camera setting
        /// </summary>
        public async Task<uint> GetSettingAsync(uint propertyId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/cameras/settings/{propertyId}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SettingResult>(json);
            return result?.Value ?? 0;
        }

        /// <summary>
        /// Set camera setting
        /// </summary>
        public async Task<bool> SetSettingAsync(uint propertyId, uint value)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/cameras/settings/{propertyId}?value={value}", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available settings list
        /// </summary>
        public async Task<List<uint>> GetSettingsListAsync(uint propertyId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/cameras/settings/{propertyId}/list");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SettingsListResult>(json);
            return result?.AvailableValues ?? new List<uint>();
        }

        // Can use using statement to dispose HttpClient ( it will dispose when out of scope )
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
