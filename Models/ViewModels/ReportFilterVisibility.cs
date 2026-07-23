namespace ClockItSystem.Models.ViewModels
{
    public class ReportFilterVisibility
    {
        public bool ShowClient { get; set; }

        public bool ShowSite { get; set; }

        public bool ShowProgramme { get; set; }

        public bool ShowStudent { get; set; }

        public bool ShowDateFrom { get; set; } = true;

        public bool ShowDateTo { get; set; } = true;
    }
}