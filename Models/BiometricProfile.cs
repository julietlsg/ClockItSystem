namespace ClockItSystem.Models
{
    public class BiometricProfile
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        public string BiometricType { get; set; } = null!;

        public string? FaceImagePath { get; set; }

        public string? BiometricTemplate { get; set; }

        // ZK Fingerprint Template
        public string? FingerprintTemplate { get; set; }

        // Optional device tracking
        public string? DeviceSerialNumber { get; set; }
        public string? DeviceVendor { get; set; }

        public string? DeviceModel { get; set; }

        public string? TemplateFormat { get; set; }

        public bool IsVerified { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        public DateTime? VerifiedAt { get; set; }
    }
}