using ClockItSystem.Data;
using ClockItSystem.Models.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Project Manager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpGet]
        public async Task<IActionResult> OfficialAttendance(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var records = await GetReportRecordsAsync(selectedDate, "Approved");
            return View(records);
        }

        [HttpGet]
        public async Task<IActionResult> PendingApprovals(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var records = await GetReportRecordsAsync(selectedDate, "PendingApproval");
            return View(records);
        }

        [HttpGet]
        public async Task<IActionResult> RejectedAttendance(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var records = await GetReportRecordsAsync(selectedDate, "Rejected");
            return View(records);
        }

        [HttpGet]
        public async Task<IActionResult> ExportOfficialExcel(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "Approved");

            return GenerateExcel(records, "Official Attendance", $"OfficialAttendance_{selectedDate:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPendingExcel(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "PendingApproval");

            return GenerateExcel(records, "Pending Approvals", $"PendingApprovals_{selectedDate:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportRejectedExcel(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "Rejected");

            return GenerateExcel(records, "Rejected Attendance", $"RejectedAttendance_{selectedDate:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportOfficialPdf(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "Approved");

            return GeneratePdf(records, "Official Attendance Report", $"OfficialAttendance_{selectedDate:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPendingPdf(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "PendingApproval");

            return GeneratePdf(records, "Pending Approvals Report", $"PendingApprovals_{selectedDate:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportRejectedPdf(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;
            var records = await GetReportRecordsAsync(selectedDate, "Rejected");

            return GeneratePdf(records, "Rejected Attendance Report", $"RejectedAttendance_{selectedDate:yyyyMMdd}.pdf");
        }

        private async Task<List<AttendanceReportViewModel>> GetReportRecordsAsync(DateTime selectedDate, string status)
        {
            return await _context.AttendanceRecords
                .Include(x => x.Student)
                .Where(x =>
                    x.AttendanceDate.Date == selectedDate &&
                    x.Status == status)
                .OrderBy(x => x.Student.LastName)
                .Select(x => new AttendanceReportViewModel
                {
                    AttendanceRecordId = x.Id,
                    StudentNumber = x.Student.StudentNumber,
                    StudentName = x.Student.FirstName + " " + x.Student.LastName,
                    ProgrammeOrCourse = x.Student.ProgrammeOrCourse,
                    AttendanceDate = x.AttendanceDate,
                    ClockTime = x.ClockTime,
                    VerificationMethod = x.VerificationMethod,
                    VerificationScore = x.VerificationScore,
                    Status = x.Status,
                    CapturedImagePath = x.CapturedImagePath,
                    ApprovedBy = _context.AttendanceApprovals
                        .Where(a => a.AttendanceRecordId == x.Id)
                        .OrderByDescending(a => a.ApprovedAt)
                        .Select(a => a.ApprovedByUserId)
                        .FirstOrDefault(),
                    ApprovedAt = _context.AttendanceApprovals
                        .Where(a => a.AttendanceRecordId == x.Id)
                        .OrderByDescending(a => a.ApprovedAt)
                        .Select(a => (DateTime?)a.ApprovedAt)
                        .FirstOrDefault(),
                    Comment = _context.AttendanceApprovals
                        .Where(a => a.AttendanceRecordId == x.Id)
                        .OrderByDescending(a => a.ApprovedAt)
                        .Select(a => a.Comment)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        private FileResult GenerateExcel(List<AttendanceReportViewModel> records, string worksheetName, string fileName)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(worksheetName);

            worksheet.Cell(1, 1).Value = worksheetName;
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;

            var headers = new[]
            {
                "Student No.",
                "Student",
                "Programme/ Course",
                "Date",
                "Clock Time",
                "Method",
                "Score",
                "Status",
                "Reviewed By",
                "Reviewed At",
                "Comment"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#071B3A");
                worksheet.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var row = 4;

            foreach (var item in records)
            {
                worksheet.Cell(row, 1).Value = item.StudentNumber;
                worksheet.Cell(row, 2).Value = item.StudentName;
                worksheet.Cell(row, 3).Value = item.ProgrammeOrCourse;
                worksheet.Cell(row, 4).Value = item.AttendanceDate.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 5).Value = item.ClockTime.ToString("HH:mm");
                worksheet.Cell(row, 6).Value = item.VerificationMethod;
                worksheet.Cell(row, 7).Value = item.VerificationScore?.ToString("0.00") ?? "-";
                worksheet.Cell(row, 8).Value = item.Status;
                worksheet.Cell(row, 9).Value = item.ApprovedBy;
                worksheet.Cell(row, 10).Value = item.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
                worksheet.Cell(row, 11).Value = item.Comment;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private FileResult GeneratePdf(List<AttendanceReportViewModel> records, string reportTitle, string fileName)
        {
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4.Landscape());

                    page.Header()
                        .Text(reportTitle)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Student No.").Bold();
                            header.Cell().Text("Student").Bold();
                            header.Cell().Text("Class").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Time").Bold();
                            header.Cell().Text("Method").Bold();
                            header.Cell().Text("Score").Bold();
                            header.Cell().Text("Status").Bold();
                        });

                        foreach (var item in records)
                        {
                            table.Cell().Text(item.StudentNumber);
                            table.Cell().Text(item.StudentName);
                            table.Cell().Text(item.ProgrammeOrCourse ?? "-");
                            table.Cell().Text(item.AttendanceDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text(item.ClockTime.ToString("HH:mm"));
                            table.Cell().Text(item.VerificationMethod);
                            table.Cell().Text(item.VerificationScore?.ToString("0.00") ?? "-");
                            table.Cell().Text(item.Status);
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated on ");
                            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}