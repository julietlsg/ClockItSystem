using ClockItSystem.ScannerAgent.DTOs;
using libzkfpcsharp;

namespace ClockItSystem.ScannerAgent.Services;

public class ZkFingerprintService : IFingerprintService
{
    private IntPtr _deviceHandle = IntPtr.Zero;
    private IntPtr _dbHandle = IntPtr.Zero;

    public ZkFingerprintService()
    {
        zkfp2.Init();

        _deviceHandle = zkfp2.OpenDevice(0);

        _dbHandle = zkfp2.DBInit();
    }

    public Task<FingerprintCaptureResponse> CaptureAsync()
    {
        try
        {
            byte[] fpBuffer = new byte[512 * 512];
            byte[] capTmp = new byte[2048];

            int cbCapTmp = 2048;

            var timeout = DateTime.Now.AddSeconds(15);

            while (DateTime.Now < timeout)
            {
                int ret = zkfp2.AcquireFingerprint(
                    _deviceHandle,
                    fpBuffer,
                    capTmp,
                    ref cbCapTmp);

                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    string template =
                        zkfp2.BlobToBase64(capTmp, cbCapTmp);

                    return Task.FromResult(
                        new FingerprintCaptureResponse
                        {
                            Success = true,
                            Template = template,
                            Message = "Fingerprint captured successfully."
                        });
                }

                Thread.Sleep(200);
            }

            return Task.FromResult(
                new FingerprintCaptureResponse
                {
                    Success = false,
                    Message = "Capture timeout."
                });
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                new FingerprintCaptureResponse
                {
                    Success = false,
                    Message = ex.Message
                });
        }
    }
}