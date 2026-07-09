using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models
{
    public class Site
    {
        public int SiteId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SiteName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SiteCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Client Client { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}