using static ClockItSystem.Models.Enums.DataEnums;

public class ReportFilterViewModel
{
    public ReportType ReportType { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int? ClientId { get; set; }

    public int? SiteId { get; set; }

    public int? StudentId { get; set; }

    public string? Programme { get; set; }
}