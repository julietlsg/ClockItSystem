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

    private class CaptureResponse
    {
        public bool Success { get; set; }

        public string? Template { get; set; }

        public string? Message { get; set; }
    }
}