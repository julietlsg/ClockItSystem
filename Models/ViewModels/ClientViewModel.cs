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

        [Required(ErrorMessage = "Client Contact Person is required.")]
        [Display(Name = "Contact Person")]
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Client Email is required.")]
        [Display(Name = "Email Address")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Client Contact Number is required.")]
        [Display(Name = "Telephone")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Client Client Address is required.")]
        [Display(Name = "Client Address")]
        [StringLength(150)]
        public string? Address { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}