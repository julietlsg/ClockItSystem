namespace ClockItSystem.Models.ViewModels.Reports
{
    public class StudentProfileReportViewModel
    {
        // Student Information
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public string? ContactNumber { get; set; }
        public string? ProgrammeOrCourse { get; set; }
        public bool IsActive { get; set; }

        // Organisation
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public int? SiteId { get; set; }
        public string? SiteName { get; set; }

        // Banking
        public int? BankId { get; set; }
        public string? BankName { get; set; }
        public int? BankBranchId { get; set; }
        public string? BranchName { get; set; }
        public int? AccountTypeId { get; set; }
        public string? AccountTypeName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }

        // Biometrics
        public string? FaceImagePath { get; set; }
        public bool FaceEnrolled { get; set; }
        public bool FingerprintEnrolled { get; set; }
        public string? BiometricStatus { get; set; }
        public DateTime? LastVerificationDate { get; set; }

        // Attendance Summary
        public int TotalAttendances { get; set; }
        public int ApprovedAttendances { get; set; }
        public DateTime? LastAttendanceDate { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Report Information
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string ReportTitle { get; set; } = "Student Profile Report";
    }
}
