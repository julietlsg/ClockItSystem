using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Models.ViewModels
{
    public class DailyAttendanceRowViewModel
    {
        #region Attendance Record

        public int AttendanceRecordId { get; set; }

        public int StudentId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string? ProgrammeOrCourse { get; set; }

        public DateTime AttendanceDate { get; set; }

        public DateTime ClockTime { get; set; }

        public string VerificationMethod { get; set; } = string.Empty;

        public decimal? VerificationScore { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? CapturedImagePath { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string SiteName { get; set; } = string.Empty;

        public int ClientId { get; set; }

        public int SiteId { get; set; }
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        #endregion

        #region Filters

        public int? SelectedClientId { get; set; }

        public int? SelectedSiteId { get; set; }

        public string? SelectedProgramme { get; set; }

        public string? SearchTerm { get; set; }

        public DateTime? SelectedDate { get; set; }

        #endregion

        #region Dropdowns

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();

        public List<SelectListItem> Programmes { get; set; } = new();

        #endregion

        #region Pagination

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalRecords / PageSize);

        #endregion

    }
}
