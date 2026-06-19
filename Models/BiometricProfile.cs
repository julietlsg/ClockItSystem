namespace ClockItSystem.Models
{
    public class BiometricProfile
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        public string BiometricType { get; set; } = "Face";

        public string? FaceImagePath { get; set; }

        public string? BiometricTemplate { get; set; }
        // This will store the face descriptor JSON from face-api.js

        public string? FingerprintTemplate { get; set; }

        public string? DeviceSerialNumber { get; set; }

        public bool IsVerified { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.Now;
    }
}