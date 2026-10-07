using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClockItSystem.Models
{
    public class ClientStipendRate
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyRate { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey(nameof(ClientId))]
        public virtual Client Client { get; set; } = null!;
    }
}