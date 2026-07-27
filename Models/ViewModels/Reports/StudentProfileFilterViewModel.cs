using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Models.ViewModels.Reports
{
    public class StudentProfileFilterViewModel
    {
        public int? ClientId { get; set; }

        public int? SiteId { get; set; }

        public int? StudentId { get; set; }

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();

        public List<SelectListItem> Students { get; set; } = new();
    }
}
