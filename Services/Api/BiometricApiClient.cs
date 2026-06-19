using System.Net.Http.Json;
using ClockItSystem.Models.Config;
using Microsoft.Extensions.Options;

namespace ClockItSystem.Services.Api
{
    public class BiometricApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigSettings _settings;

        public BiometricApiClient(
            HttpClient httpClient,
            IOptions<ConfigSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            _httpClient.BaseAddress =
                new Uri(_settings.BaseUrl);
        }

        public async Task<bool> EnrollFaceAsync(
            int studentId,
            string descriptorJson)
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    "/api/biometric/enroll",
                    new
                    {
                        studentId,
                        biometricType = "Face",
                        template = descriptorJson,
                        deviceVendor = "FaceAPI",
                        deviceModel = "Browser",
                        templateFormat = "FACE_DESCRIPTOR"
                    });

            return response.IsSuccessStatusCode;
        }
    }
}