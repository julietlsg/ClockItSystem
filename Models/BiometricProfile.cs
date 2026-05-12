namespace ClockItSystem.Models
{
    public class BiometricProfile
    {
        public int Id { get; set; }

        public int LearnerId { get; set; }
        public Learner Learner { get; set; } = null!;

        public string BiometricType { get; set; } = string.Empty;
        // Face / Fingerprint

        public string? FaceImagePath { get; set; }

        public string? BiometricTemplate { get; set; }
        // Store face embedding/template reference, not raw biometric data where possible.

        public bool IsVerified { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}
