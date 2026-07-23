using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class AccountType
    {
        public int AccountTypeId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Account Type")]
        public string AccountTypeName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<Student> Students { get; set; }
            = new List<Student>();
    }
}