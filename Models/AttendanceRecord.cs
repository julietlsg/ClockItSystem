namespace ClockItSystem.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        public int LearnerId { get; set; }
        public Learner Learner { get; set; } = null!;

        public DateTime AttendanceDate { get; set; }
        public DateTime ClockTime { get; set; }

        public string VerificationMethod { get; set; } = string.Empty;
        // Face / Fingerprint / Manual

        public decimal? VerificationScore { get; set; }

        public string Status { get; set; } = "PendingApproval";
        // PendingApproval / Approved / Rejected

        public string? CapturedImagePath { get; set; }

        public string? CreatedByUserId { get; set; }
    }
}
