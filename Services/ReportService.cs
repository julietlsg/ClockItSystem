using ClockItSystem.Data;
using ClockItSystem.Helpers;
using ClockItSystem.Models.Enums;
using ClockItSystem.Models.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<List<ReportResultViewModel>> ExecuteReportAsync(
            ReportFilterViewModel filter)
        {
            var procedure = GetStoredProcedure(filter.ReportType);

            var sql = $@"
EXEC {procedure}
     @DateFrom,
     @DateTo,
     @ClientId,
     @SiteId,
     @Programme,
     @StudentId";

            return await _context.Set<ReportResultViewModel>()
                .FromSqlRaw(
                    sql,
                    new SqlParameter("@DateFrom", (object?)filter.DateFrom ?? DBNull.Value),
                    new SqlParameter("@DateTo", (object?)filter.DateTo ?? DBNull.Value),
                    new SqlParameter("@ClientId", (object?)filter.ClientId ?? DBNull.Value),
                    new SqlParameter("@SiteId", (object?)filter.SiteId ?? DBNull.Value),
                    new SqlParameter("@Programme", (object?)filter.Programme ?? DBNull.Value),
                    new SqlParameter("@StudentId", (object?)filter.StudentId ?? DBNull.Value)
                )
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<FileResult> ExportExcelAsync(
            List<ReportResultViewModel> data,
            ReportFilterViewModel filter)
        {
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Report");

            var title = ReportExportHelper.GetReportTitle(filter.ReportType);

            var columns = ReportExportHelper.GetColumns(data);

            worksheet.Cell(1, 1).Value = title;

            worksheet.Range(1, 1, 1, Math.Max(columns.Count, 10)).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 18;

            worksheet.Cell(2, 1).Value = "Generated";
            worksheet.Cell(2, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            worksheet.Cell(3, 1).Value = "Date From";
            worksheet.Cell(3, 2).Value = filter.DateFrom?.ToString("yyyy-MM-dd") ?? "All";

            worksheet.Cell(4, 1).Value = "Date To";
            worksheet.Cell(4, 2).Value = filter.DateTo?.ToString("yyyy-MM-dd") ?? "All";

            worksheet.Cell(5, 1).Value = "Client";
            worksheet.Cell(5, 2).Value = filter.ClientId?.ToString() ?? "All";

            worksheet.Cell(6, 1).Value = "Site";
            worksheet.Cell(6, 2).Value = filter.SiteId?.ToString() ?? "All";

            worksheet.Cell(7, 1).Value = "Programme";
            worksheet.Cell(7, 2).Value = string.IsNullOrWhiteSpace(filter.Programme)
                ? "All"
                : filter.Programme;

            worksheet.Cell(8, 1).Value = "Student";
            worksheet.Cell(8, 2).Value = filter.StudentId?.ToString() ?? "All";

            for (int i = 2; i <= 8; i++)
            {
                worksheet.Cell(i, 1).Style.Font.Bold = true;
            }

            const int headerRow = 10;

            for (int i = 0; i < columns.Count; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);

                cell.Value = columns[i].Header;

                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#071B3A");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
            }

            int row = headerRow + 1;

            foreach (var item in data)
            {
                for (int col = 0; col < columns.Count; col++)
                {
                    worksheet.Cell(row, col + 1).Value =
                        ReportExportHelper.GetValue(item, columns[col])?.ToString();
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            worksheet.RangeUsed().SetAutoFilter();

            worksheet.SheetView.FreezeRows(headerRow);

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return Task.FromResult<FileResult>(
                new FileContentResult(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName =
                        $"{title.Replace(" ", "")}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                });
        }
        public Task<FileResult> ExportPdfAsync(
    List<ReportResultViewModel> data,
    ReportFilterViewModel filter)
        {
            var title = ReportExportHelper.GetReportTitle(filter.ReportType);

            var columns = ReportExportHelper.GetColumns(data);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Text(title)
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        header.Item().PaddingTop(10);

                        header.Item().Text($"Generated : {DateTime.Now:yyyy-MM-dd HH:mm}");

                        header.Item().Text($"Date From : {(filter.DateFrom?.ToString("yyyy-MM-dd") ?? "All")}");

                        header.Item().Text($"Date To : {(filter.DateTo?.ToString("yyyy-MM-dd") ?? "All")}");

                        header.Item().Text($"Client : {(filter.ClientId?.ToString() ?? "All")}");

                        header.Item().Text($"Site : {(filter.SiteId?.ToString() ?? "All")}");

                        header.Item().Text($"Programme : {(string.IsNullOrWhiteSpace(filter.Programme) ? "All" : filter.Programme)}");

                        header.Item().Text($"Student : {(filter.StudentId?.ToString() ?? "All")}");
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(col =>
                        {
                            for (int i = 0; i < columns.Count; i++)
                            {
                                col.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var column in columns)
                            {
                                header.Cell()
                                    .Background("#071B3A")
                                    .Padding(5)
                                    .Border(1)
                                    .Text(column.Header)
                                    .FontColor(Colors.White)
                                    .Bold()
                                    .FontSize(10);
                            }
                        });

                        foreach (var row in data)
                        {
                            foreach (var column in columns)
                            {
                                table.Cell()
                                    .Border(1)
                                    .Padding(4)
                                    .Text(
                                        ReportExportHelper
                                            .GetValue(row, column)?
                                            .ToString() ?? string.Empty)
                                    .FontSize(9);
                            }
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                            text.Span("    Generated ");
                            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        });
                });
            }).GeneratePdf();

            return Task.FromResult<FileResult>(
                new FileContentResult(pdf, "application/pdf")
                {
                    FileDownloadName =
                        $"{title.Replace(" ", "")}_{DateTime.Now:yyyyMMddHHmmss}.pdf"
                });
        }
        private static string GetStoredProcedure(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.NonAttendance =>
                    "usp_Report_NonAttendance",

                ReportType.StudentAttendance =>
                    "usp_Report_StudentAttendance",

                ReportType.AttendanceRegister =>
                    "usp_Report_ClientStudentTotals",

                ReportType.ClientAttendanceSummary =>
                    "usp_Report_ClientAttendanceSummary",

                ReportType.SiteAttendanceSummary =>
                    "usp_Report_SiteAttendanceSummary",

                ReportType.ProgrammeAttendanceSummary =>
                    "usp_Report_ProgrammeAttendanceSummary",


                _ => throw new ArgumentOutOfRangeException(
                    nameof(reportType),
                    reportType,
                    "Unknown report type.")
            };
        }
    }
}