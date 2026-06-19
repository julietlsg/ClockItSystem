using libzkfpcsharp;

namespace ClockItSystem.Services
{
    public class ZkFingerprintScannerService
    {
        private IntPtr mDevHandle = IntPtr.Zero;
        private IntPtr mDBHandle = IntPtr.Zero;

        public bool Initialize()
        {
            return zkfp2.Init() ==
                   zkfperrdef.ZKFP_ERR_OK;
        }

        public bool OpenDevice()
        {
            mDevHandle =
                zkfp2.OpenDevice(0);

            return mDevHandle != IntPtr.Zero;
        }

        public bool InitializeDatabase()
        {
            mDBHandle =
                zkfp2.DBInit();

            return mDBHandle != IntPtr.Zero;
        }

        public void Close()
        {
            if (mDevHandle != IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
            }

            zkfp2.Terminate();
        }
    }
}