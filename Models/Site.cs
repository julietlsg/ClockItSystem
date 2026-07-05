namespace ClockItSystem.Models
{
    public class Site
    {
        public int SiteId { get; set; }
        public  int ClientId { get; set; }
        public required string SiteName { get; set; }
        public required string SiteCode { get; set; }
        public DateTime DateCreated { get; set; }

    }
}
