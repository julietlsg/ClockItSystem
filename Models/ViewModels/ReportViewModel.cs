using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Models.ViewModels
{
    public class ReportViewModel
    {
        public ReportFilterViewModel Filter { get; set; } = new();

        public List<ReportResultViewModel> Results { get; set; } = new();

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();

        public List<SelectListItem> Students { get; set; } = new();

        public List<SelectListItem> ReportTypes { get; set; } = new();

        public List<SelectListItem> Programmes { get; set; } = new();

        public ReportFilterVisibility FilterVisibility { get; set; } = new ReportFilterVisibility();
    }
}