namespace ClockItSystem.Models.ViewModels
{
    public class DailyAttendanceViewModel
    {
        public AttendanceFilterViewModel Filter { get; set; } = new();

        public List<DailyApprovalViewModel> Records { get; set; } = new();

        public PaginationViewModel Pagination { get; set; } = new();
    }
}