using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.ViewModels
{
    public class PaymentRunViewModel
    {
        [Required(ErrorMessage = "Please select a client.")]
        public int? ClientId { get; set; }

        [Required(ErrorMessage = "Please select a month.")]
        public int? Year { get; set; }

        [Required(ErrorMessage = "Please select a month.")]
        public int? Month { get; set; }

        public List<SelectListItem> Clients { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> Years { get; set; }
            = new List<SelectListItem>();

        public List<SelectListItem> Months { get; set; }
            = new List<SelectListItem>();
    }
}