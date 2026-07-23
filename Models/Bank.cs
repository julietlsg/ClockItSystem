using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class Bank
    {
        public int BankId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Bank")]
        public string BankName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<BankBranch> BankBranches { get; set; }
            = new List<BankBranch>();

        public ICollection<Student> Students { get; set; }
            = new List<Student>();
    }
}