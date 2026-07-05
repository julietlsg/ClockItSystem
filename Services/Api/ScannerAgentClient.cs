using System.Net.Http.Json;
using ClockItSystem.Models.Config;
using Microsoft.Extensions.Options;

namespace ClockItSystem.Services.Api;

public class ScannerAgentClient
{
    private readonly HttpClient _httpClient;

    public ScannerAgentClient(
        HttpClient httpClient,
        IOptions<ConfigSettings> settings)
    {
        _httpClient = httpClient;

        _httpClient.BaseAddress =
            new Uri(settings.Value.ScannerBaseUrl);
    }

    public async Task<string?> CaptureFingerprintAsync()
    {
        var response =
            await _httpClient.PostAsync(
                "/api/fingerprint/capture",
                null);

        if (!response.IsSuccessStatusCode)
            return null;

        var result =
            await response.Content
                .ReadFromJsonAsync<CaptureResponse>();

        return result?.Template;
    }

    public async Task<(bool Success, int StudentId, int Score)>
    IdentifyFingerprintAsync(
        List<FingerprintTemplateDto> templates)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "/api/fingerprint/identify",
                new
                {
                    templates
                });

        if (!response.IsSuccessStatusCode)
        {
            return (false, 0, 0);
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<IdentifyResponse>();

        if (result == null)
        {
            return (false, 0, 0);
        }

        return (
            result.Success,
            result.StudentId,
            result.Score
        );
    }

    private class CaptureResponse
    {
        public bool Success { get; set; }

        public string? Template { get; set; }

        public string? Message { get; set; }
    }
    private class IdentifyResponse
    {
        public bool Success { get; set; }

        public int StudentId { get; set; }

        public int Score { get; set; }
    }

    public class FingerprintTemplateDto
    {
        public int StudentId { get; set; }

        public string Template { get; set; } = string.Empty;
    }
}