namespace ClockItSystem.Models.ViewModels
{
    public class ActionItem
    {
        public string Text { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public int RouteId { get; set; }

        public string ButtonClass { get; set; } = "dropdown-item";

        public bool IsPost { get; set; }

        public string? ConfirmMessage { get; set; }
    }
}