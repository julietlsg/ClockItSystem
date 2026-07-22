using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class ReportResultViewModel
    {

        [Display(Name = "Client")]
        public string? ClientName { get; set; }

        [Display(Name = "Site")]
        public string? SiteName { get; set; }

        [Display(Name = "Student Number")]
        public string? StudentNumber { get; set; }

        [Display(Name = "Student")]
        public string? StudentName { get; set; }

        [Display(Name = "Programme")]
        public string? ProgrammeOrCourse { get; set; }

        [Display(Name = "Attendance Date")]
        public DateTime? AttendanceDate { get; set; }

        [Display(Name = "Clock Time")]
        public TimeSpan? ClockTime { get; set; }

        [Display(Name = "Verification Method")]
        public string? VerificationMethod { get; set; }

        [Display(Name = "Verification Score")]
        public decimal? VerificationScore { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Total Days")]
        public int? TotalDays { get; set; }

        [Display(Name = "Attendance %")]
        public decimal? AttendancePercentage { get; set; }
    }
}
