using System;
using System.Collections.Generic;

namespace ClockItSystem.ViewModels
{
    public class PaymentReviewViewModel
    {
        public List<PaymentReviewItemViewModel> PaymentRuns { get; set; }
            = new List<PaymentReviewItemViewModel>();
    }

    public class PaymentReviewItemViewModel
    {
        public int PaymentRunId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public int TotalStudents { get; set; }

        public int TotalEligibleDays { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }
}