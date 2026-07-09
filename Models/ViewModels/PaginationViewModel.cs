namespace ClockItSystem.Models.ViewModels
{
    public class PaginationViewModel
    {
        public PagedResult Pagination { get; set; } = new();

        public string Action { get; set; } = "Index";

        public Dictionary<string, string?> RouteValues { get; set; }
            = new();
    }
}