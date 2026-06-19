using ClockItSystem.ScannerAgent.DTOs;

namespace ClockItSystem.ScannerAgent.Services
{
    public interface IFingerprintService
    {
        Task<FingerprintCaptureResponse> CaptureAsync();
    }
}
