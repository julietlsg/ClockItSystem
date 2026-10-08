using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.ViewModels
{
    public class PaymentRunViewModel
    {
        [Required(ErrorMessage = "Please select a client.")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Please select a payment period.")]
        public DateTime? PaymentPeriod { get; set; }

        [Required(ErrorMessage = "Please select a payment date.")]
        public DateTime? PaymentDate { get; set; }

        public List<SelectListItem> Clients { get; set; }
            = new List<SelectListItem>();
    }
}