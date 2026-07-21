using System.ComponentModel.DataAnnotations;
using static ClockItSystem.Models.Enums.DataEnums;

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
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        public string? SupportingDocumentPath { get; set; }
        public AttendanceRejectionReason Reason { get; set; }
    }
}
