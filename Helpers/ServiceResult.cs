namespace ClockItSystem.Helpers
{
    public class ServiceResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? Id { get; set; }
    }
}
