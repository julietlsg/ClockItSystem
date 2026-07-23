using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class StudentSearchViewModel
    {

        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Client")]
        public int? ClientId { get; set; }

        [Display(Name = "Site")]
        public int? SiteId { get; set; }

        [Display(Name = "Programme")]
        public string? ProgrammeOrCourse { get; set; }

        [Display(Name = "Status")]
        public bool? IsActive { get; set; }

        public List<SelectListItem> Clients { get; set; } = new();

        public List<SelectListItem> Sites { get; set; } = new();
        public List<SelectListItem> Programmes { get; set; } = new();

        public IEnumerable<StudentViewModel> Students { get; set; } = new List<StudentViewModel>();
    }
}