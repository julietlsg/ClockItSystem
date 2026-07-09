using ClockItSystem.Models.ResultModels;

namespace ClockItSystem.Interfaces
{
    public interface IBiometricService
    {
        Task<BiometricVerificationResult> VerifyAsync(
            string biometricTemplate);
    }
}