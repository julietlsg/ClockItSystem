namespace ClockItSystem.Models.ViewModels
{
    public class PagedRequest
    {
        // Search
        public string? SearchTerm { get; set; }

        // Common Status Filter
        public bool? IsActive { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        // Reporting Filters
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        // Multi-tenancy Filters
        public int? ClientId { get; set; }

        public int? SiteId { get; set; }
    }
}