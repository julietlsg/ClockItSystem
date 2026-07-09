namespace ClockItSystem.Models.ViewModels
{
    public class SearchFilterViewModel
    {
        public string Action { get; set; } = "Index";

        public PagedRequest Filter { get; set; } = new();

        public bool ShowStatus { get; set; } = true;

        public bool ShowClient { get; set; }

        public bool ShowSite { get; set; }
    }
}