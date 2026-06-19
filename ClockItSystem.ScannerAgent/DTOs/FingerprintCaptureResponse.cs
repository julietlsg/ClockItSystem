namespace ClockItSystem.ScannerAgent.DTOs
{
    public class FingerprintCaptureResponse
    {
        public bool Success { get; set; }

        public string? Template { get; set; }

        public string? Message { get; set; }
    }
}
