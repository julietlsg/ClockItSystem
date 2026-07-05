using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class SiteViewModel
    {
        public int SiteId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [Display(Name = "Site Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
