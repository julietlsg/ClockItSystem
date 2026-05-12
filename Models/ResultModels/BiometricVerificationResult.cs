namespace ClockItSystem.Models.ResultModels
{
    public class BiometricVerificationResult
    {
        public bool IsMatch { get; set; }
        public int? LearnerId { get; set; }
        public decimal Score { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
