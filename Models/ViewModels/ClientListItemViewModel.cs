namespace ClockItSystem.Models.ViewModels
{
    public class ClientListItemViewModel
    {
        public int ClientId { get; set; }

        public string Name { get; set; } = "";

        public string Code { get; set; } = "";

        public string? ContactPerson { get; set; }

        public string? Email { get; set; }

        public int SiteCount { get; set; }

        public bool IsActive { get; set; }
    }
}