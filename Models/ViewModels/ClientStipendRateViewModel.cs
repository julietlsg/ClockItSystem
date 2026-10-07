using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class ClientStipendRateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a client.")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Please enter the daily stipend rate.")]
        [Range(0.01, 999999.99, ErrorMessage = "Please enter a valid stipend rate.")]
        [Display(Name = "Daily Stipend Rate")]
        public decimal DailyRate { get; set; }

        [Required(ErrorMessage = "Please select an effective date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Effective From")]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Effective To")]
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ClientName { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public List<SelectListItem> Clients { get; set; } = new();
    }
}