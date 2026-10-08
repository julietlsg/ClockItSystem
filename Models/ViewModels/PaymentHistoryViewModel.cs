using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.ViewModels
{
    public class PaymentHistoryViewModel
    {
        public int? ClientId { get; set; }

        public string? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<SelectListItem> Clients { get; set; }
            = new();

        public List<PaymentHistoryItemViewModel> PaymentRuns { get; set; }
            = new();
    }

    public class PaymentHistoryItemViewModel
    {
        public int PaymentRunId { get; set; }

        public string ClientName { get; set; }
            = string.Empty;

        public DateTime PaymentDate { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public int TotalStudents { get; set; }

        public int TotalEligibleDays { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string? NetcashUploadStatus { get; set; }

        public string? NetcashFileToken { get; set; }

        public string? NetcashUploadReport { get; set; }

        public DateTime? NetcashReportedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }
}