namespace ClockItSystem.Models.ViewModels
{
    public class AdminHeaderViewModel
    {
        public string Title { get; set; } = "";

        public string? SubTitle { get; set; }

        public string Icon { get; set; } = "bi bi-gear";

        public string? ButtonText { get; set; }

        public string? ButtonAction { get; set; }
    }
}