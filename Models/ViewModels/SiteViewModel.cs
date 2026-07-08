
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class SiteViewModel
    {
        public int SiteId { get; set; }

        [Display(Name = "Client")]
        [Required(ErrorMessage = "Please select a client.")]
        public int ClientId { get; set; }

        [Display(Name = "Site Name")]
        [Required(ErrorMessage = "Site name is required.")]
        [StringLength(100)]
        public string SiteName { get; set; } = string.Empty;

        [Display(Name = "Site Code")]
        [Required(ErrorMessage = "Site code is required.")]
        [StringLength(20)]
        public string SiteCode { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Address { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Clients { get; set; }
            = new();
    }
}