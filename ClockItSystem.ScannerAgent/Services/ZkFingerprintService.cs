using ClockItSystem.ScannerAgent.DTOs;
using libzkfpcsharp;

namespace ClockItSystem.ScannerAgent.Services;

public class ZkFingerprintService : IFingerprintService
{
    public async Task<FingerprintCaptureResponse> CaptureAsync()
    {
        try
        {
            return new FingerprintCaptureResponse
            {
                Success = false,
                Message = "Capture not implemented yet."
            };
        }
        catch (Exception ex)
        {
            return new FingerprintCaptureResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}