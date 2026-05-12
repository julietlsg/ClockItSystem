using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Programme/Course")]
        public string? ProgrammeOrCourse { get; set; }

        [Display(Name = "Guardian Name")]
        public string? GuardianName { get; set; }

        [Display(Name = "Guardian Phone")]
        public string? GuardianPhone { get; set; }

        [Display(Name = "Face Image")]
        public string? FaceImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public ICollection<BiometricProfile> BiometricProfiles { get; set; } = new List<BiometricProfile>();

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}