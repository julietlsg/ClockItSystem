using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Models.ViewModels
{
    public class AttendanceReportViewModel
    {
        public int AttendanceRecordId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string? ProgrammeOrCourse { get; set; }

        public DateTime AttendanceDate { get; set; }

        public DateTime ClockTime { get; set; }

        public string VerificationMethod { get; set; } = string.Empty;

        public decimal? VerificationScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? CapturedImagePath { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? Comment { get; set; }
        public int? ClientId { get; set; }

        public int? SiteId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string SiteName { get; set; } = string.Empty;

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();
    }
}