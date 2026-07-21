using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Models.ViewModels
{
    public class AttendanceFilterViewModel
    {
        public DateTime SelectedDate { get; set; }

        public int? ClientId { get; set; }

        public int? SiteId { get; set; }

        public string? Programme { get; set; }

        public string? SearchTerm { get; set; }

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();

        public List<SelectListItem> Programmes { get; set; } = new();
    }
}