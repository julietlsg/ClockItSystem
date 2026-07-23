using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class BankBranch
    {
        public int BankBranchId { get; set; }

        [Required]
        public int BankId { get; set; }

        public Bank? Bank { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Branch")]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Display(Name = "Branch Code")]
        public string BranchCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<Student> Students { get; set; }
            = new List<Student>();
    }
}