using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class ClientViewModel
    {
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Client Name is required.")]
        [Display(Name = "Client Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client Code is required.")]
        [Display(Name = "Client Code")]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "Telephone")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}