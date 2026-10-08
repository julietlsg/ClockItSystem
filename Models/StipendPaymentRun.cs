using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClockItSystem.Models
{
    public class StipendPaymentRun
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime PeriodFrom { get; set; }

        [Required]
        public DateTime PeriodTo { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        public int TotalStudents { get; set; }

        public int TotalEligibleDays { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Draft, PendingReview, Approved, Submitted, Processed, Failed
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? NetcashFileToken { get; set; }

        public string? NetcashUploadStatus { get; set; }

        public string? NetcashUploadReport { get; set; }

        public DateTime? NetcashReportedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? FailureReason { get; set; }

        // Navigation properties

        [ForeignKey(nameof(ClientId))]
        public virtual Client Client { get; set; } = null!;

        public virtual ICollection<StipendPayment> Payments { get; set; }
            = new List<StipendPayment>();
    }
}