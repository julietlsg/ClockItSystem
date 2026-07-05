using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class ClientViewModel
    {
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Client Name is required")]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client Code is required")]
        [Display(Name = "Client Code")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Telephone")]
        public string? Phone { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
