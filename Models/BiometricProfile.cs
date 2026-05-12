namespace ClockItSystem.Models
{
    public class BiometricProfile
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public Student Student { get; set; } = null!;

        public string BiometricType { get; set; } = string.Empty;

        public string? FaceImagePath { get; set; }

        public string? BiometricTemplate { get; set; }

        public bool IsVerified { get; set; }

        public DateTime EnrolledAt { get; set; }
    }
}