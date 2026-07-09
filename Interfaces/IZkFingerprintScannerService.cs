namespace ClockItSystem.Interfaces
{
    public interface IZkFingerprintScannerService
    {
        bool Initialize();

        string CaptureFingerprintTemplate();

        int MatchFingerprints(
            string capturedTemplate,
            string storedTemplate);
    }
}