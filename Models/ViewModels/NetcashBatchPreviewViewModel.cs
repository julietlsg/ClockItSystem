using System;
using System.Collections.Generic;

namespace ClockItSystem.ViewModels
{
    public class NetcashBatchPreviewViewModel
    {
        public int PaymentRunId { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public int TotalPayments { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<NetcashBatchPreviewItemViewModel> Payments { get; set; }
            = new List<NetcashBatchPreviewItemViewModel>();
    }


    public class NetcashBatchPreviewItemViewModel
    {
        public int PaymentId { get; set; }

        public int StudentId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string AccountHolderName { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string BranchCode { get; set; } = string.Empty;

        public string AccountNumber { get; set; } = string.Empty;

        public string AccountType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string PaymentReference { get; set; } = string.Empty;

        public bool BankingDetailsValid { get; set; }

        public string? ValidationMessage { get; set; }
    }
}