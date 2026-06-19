using ClockItSystem.Data;
using ClockItSystem.Models.ResultModels;
using ClockItSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class FingerprintBiometricService : IBiometricService
    {
        private readonly ApplicationDbContext _context;

        public FingerprintBiometricService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BiometricVerificationResult> VerifyAsync(
            string fingerprintTemplate)
        {
            var enrolledPrints =
                await _context.BiometricProfiles
                    .Include(x => x.Student)
                    .Where(x =>
                        x.BiometricType == "Fingerprint" &&
                        x.IsVerified &&
                        x.Student.IsActive)
                    .ToListAsync();

            foreach (var profile in enrolledPrints)
            {
                // ZK9500 DBMatch goes here later
            }

            return new BiometricVerificationResult
            {
                IsMatch = false,
                Message = "Fingerprint not recognised."
            };
        }
    }
}