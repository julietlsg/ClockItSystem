using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class Client
    {
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public ICollection<Site> Sites { get; set; }
            = new List<Site>();

        public ICollection<Student> Students { get; set; }
            = new List<Student>();
    }
}