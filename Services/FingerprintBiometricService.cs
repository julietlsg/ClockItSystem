using ClockItSystem.Models.ResultModels;
using ClockItSystem.Services.Interfaces;

namespace ClockItSystem.Services
{
    public class FingerprintBiometricService : IBiometricService
    {
        public Task<BiometricVerificationResult> VerifyAsync(string fingerprintData) { throw new NotImplementedException(); }
    }
}
