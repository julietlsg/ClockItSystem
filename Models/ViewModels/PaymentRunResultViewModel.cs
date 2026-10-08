namespace ClockItSystem.ViewModels
{
    public class PaymentRunResultViewModel
    {
        public int PaymentRunId { get; set; }

        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public int TotalStudents { get; set; }

        public int TotalEligibleDays { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<PaymentRunStudentViewModel> Students { get; set; }
            = new List<PaymentRunStudentViewModel>();
    }

    public class PaymentRunStudentViewModel
    {
        public int StudentId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public int EligibleAttendanceDays { get; set; }

        public int LeaveDays { get; set; }

        public int SickLeaveDays { get; set; }

        public int FamilyResponsibilityLeaveDays { get; set; }

        public int TotalEligibleDays { get; set; }

        public decimal DailyRate { get; set; }

        public decimal StipendAmount { get; set; }

        public string? BankName { get; set; }

        public string? BranchCode { get; set; }

        public string? AccountNumber { get; set; }

        public string? AccountType { get; set; }

        public string? AccountHolderName { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}