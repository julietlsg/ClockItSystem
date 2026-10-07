using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClockItSystem.Models
{
    public class StipendPayment
    {
        public int Id { get; set; }

        [Required]
        public int PaymentRunId { get; set; }

        [Required]
        public int StudentId { get; set; }

        // Attendance/payment breakdown

        public int EligibleAttendanceDays { get; set; }

        public int LeaveDays { get; set; }

        public int SickLeaveDays { get; set; }

        public int FamilyResponsibilityLeaveDays { get; set; }

        public int TotalEligibleDays { get; set; }

        // Rate and calculated amount

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyRate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StipendAmount { get; set; }

        // Banking snapshot
        // These values are copied at payment-run generation time
        // so historical payment runs are not changed if the
        // student's banking details are subsequently edited.

        [MaxLength(200)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? BranchCode { get; set; }

        [MaxLength(11)]
        public string? AccountNumber { get; set; }

        [MaxLength(100)]
        public string? AccountType { get; set; }

        [MaxLength(200)]
        public string? AccountHolderName { get; set; }

        // Netcash/account reference

        [MaxLength(100)]
        public string? NetcashAccountReference { get; set; }

        // Payment status

        /// <summary>
        /// Pending, Validated, Approved, Submitted, Paid, Failed
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public string? FailureReason { get; set; }

        // Navigation properties

        [ForeignKey(nameof(PaymentRunId))]
        public virtual StipendPaymentRun PaymentRun { get; set; } = null!;

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;
    }
}