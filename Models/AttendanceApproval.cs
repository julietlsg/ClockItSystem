namespace ClockItSystem.Models
{
    public class AttendanceApproval
    {
        public int Id { get; set; }

        public int AttendanceRecordId { get; set; }
        public AttendanceRecord AttendanceRecord { get; set; } = null!;

        public string ApprovedByUserId { get; set; } = string.Empty;

        public bool IsApproved { get; set; }
        public string? Comment { get; set; }

        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    }
}
