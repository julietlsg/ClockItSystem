namespace ClockItSystem.Models
{
    public class Learner
    {
        public int Id { get; set; }

        public string StudentNumber { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string GradeOrClass { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BiometricProfile> BiometricProfiles { get; set; } = new List<BiometricProfile>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
