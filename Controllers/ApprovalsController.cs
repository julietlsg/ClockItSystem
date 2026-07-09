using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Facilitator")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public ApprovalsController(ApplicationDbContext context, IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<IActionResult> Daily(
            DateTime? date,
            int? clientId,
            int? siteId,
            string? programme,
            string? searchTerm,
            int page = 1)
                {
                    const int pageSize = 10;

                    var selectedDate = date?.Date ?? DateTime.Today;

                    var query = _context.AttendanceRecords
                        .Include(x => x.Student)
                        .Include(x => x.Client)
                        .Include(x => x.Site)
                        .Where(x => x.AttendanceDate.Date == selectedDate)
                        .AsQueryable();

                    if (clientId.HasValue)
                    {
                        query = query.Where(x => x.ClientId == clientId);
                    }

                    if (siteId.HasValue)
                    {
                        query = query.Where(x => x.SiteId == siteId);
                    }

                    if (!string.IsNullOrWhiteSpace(programme))
                    {
                        query = query.Where(x =>
                            x.Student.ProgrammeOrCourse == programme);
                    }

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        query = query.Where(x =>
                            x.Student.StudentNumber.Contains(searchTerm) ||
                            x.Student.FirstName.Contains(searchTerm) ||
                            x.Student.LastName.Contains(searchTerm));
                    }

                    var totalRecords = await query.CountAsync();

                    var records = await query
                        .OrderBy(x => x.Student.LastName)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(x => new DailyApprovalViewModel
                        {
                            AttendanceRecordId = x.Id,

                            StudentId = x.StudentId,

                            StudentNumber = x.Student.StudentNumber,

                            StudentName = x.Student.FirstName + " " + x.Student.LastName,

                            ProgrammeOrCourse = x.Student.ProgrammeOrCourse,

                            AttendanceDate = x.AttendanceDate,

                            ClockTime = x.ClockTime,

                            VerificationMethod = x.VerificationMethod,

                            VerificationScore = x.VerificationScore,

                            Status = x.Status,

                            CapturedImagePath = x.CapturedImagePath,

                            ClientId = x.ClientId,

                            SiteId = x.SiteId,

                            ClientName = x.Client != null
                                ? x.Client.Name
                                : "",

                            SiteName = x.Site != null
                                ? x.Site.SiteName
                                : "",

                            SelectedClientId = clientId,

                            SelectedSiteId = siteId,

                            SelectedProgramme = programme,

                            SearchTerm = searchTerm,

                            SelectedDate = selectedDate,

                            CurrentPage = page,

                            PageSize = pageSize,

                            TotalRecords = totalRecords
                        })
                        .ToListAsync();

                    var clients = await GetClientsAsync();
                    var sites = await GetSitesAsync(clientId);
                    var programmes = await GetProgrammesAsync();

                    if (!records.Any())
                    {
                        records.Add(new DailyApprovalViewModel
                        {
                            Clients = clients,
                            Sites = sites,
                            Programmes = programmes,

                            SelectedClientId = clientId,
                            SelectedSiteId = siteId,
                            SelectedProgramme = programme,
                            SearchTerm = searchTerm,
                            SelectedDate = selectedDate,

                            CurrentPage = page,
                            PageSize = pageSize,
                            TotalRecords = totalRecords
                        });
                    }
                    else
                    {
                        foreach (var row in records)
                        {
                            row.Clients = clients;
                            row.Sites = sites;
                            row.Programmes = programmes;

                            row.SelectedClientId = clientId;
                            row.SelectedSiteId = siteId;
                            row.SelectedProgramme = programme;
                            row.SearchTerm = searchTerm;
                            row.SelectedDate = selectedDate;

                            row.CurrentPage = page;
                            row.PageSize = pageSize;
                            row.TotalRecords = totalRecords;
                        }
                    }

                    return View(records);
                }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int attendanceRecordId)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x => x.Id == attendanceRecordId);

            if (record == null)
                return NotFound();

            record.Status = "Approved";

            _context.AttendanceApprovals.Add(new AttendanceApproval
            {
                AttendanceRecordId = record.Id,
                ApprovedByUserId = User.Identity?.Name ?? "System",
                IsApproved = true,
                Comment = "Approved",
                ApprovedAt = DateTime.Now, 
                ClientId = record.ClientId,
                SiteId   = record.SiteId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance approved successfully.";

            return RedirectToAction(nameof(Daily), new { date = record.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int attendanceRecordId,
            string reason,
            string? comment,
            IFormFile? supportingDocument)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x => x.Id == attendanceRecordId);
            var documentPath = await _fileStorageService
                .SaveAttendanceDocumentAsync(supportingDocument);

            if (record == null)
                return NotFound();

            record.Status = "Rejected";

            // Convert the selected reason to the enum
            if (!Enum.TryParse<AttendanceRejectionReason>(
                    reason,
                    ignoreCase: true,
                    out var rejectionReason))
            {
                ModelState.AddModelError("", "Please select a valid rejection reason.");

                TempData["Error"] = "Please select a valid rejection reason.";

                return RedirectToAction(nameof(Daily),
                    new { date = record.AttendanceDate.ToString("yyyy-MM-dd") });
            }

            _context.AttendanceApprovals.Add(new AttendanceApproval
            {
                AttendanceRecordId = record.Id,

                ApprovedByUserId = User.Identity?.Name ?? "System",

                IsApproved = false,

                Comment = string.IsNullOrWhiteSpace(comment)
                    ? "Rejected"
                    : comment,

                Reason = rejectionReason,

                SupportingDocumentPath = documentPath,

                ApprovedAt = DateTime.Now,

                ClientId = record.ClientId,

                SiteId = record.SiteId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance rejected successfully.";

            return RedirectToAction(nameof(Daily), new { date = record.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var records = await _context.AttendanceApprovals
                .Include(x => x.AttendanceRecord)
                .ThenInclude(a => a.Student)
                .Include(x => x.AttendanceRecord)
                .ThenInclude(a => a.Client)
                .Include(x => x.AttendanceRecord)
                .ThenInclude(a => a.Site)
                .OrderByDescending(x => x.ApprovedAt)
                .ToListAsync();

            return View(records);
        }
        private async Task<List<SelectListItem>> GetClientsAsync()
        {
            return await _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.ClientId.ToString(),
                    Text = x.Name
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSitesAsync(int? clientId)
        {
            var query = _context.Sites
                .Where(x => x.IsActive);

            if (clientId.HasValue)
            {
                query = query.Where(x => x.ClientId == clientId);
            }

            return await query
                .OrderBy(x => x.SiteName)
                .Select(x => new SelectListItem
                {
                    Value = x.SiteId.ToString(),
                    Text = x.SiteName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetProgrammesAsync()
        {
            return await _context.Students
                .Where(x => !string.IsNullOrEmpty(x.ProgrammeOrCourse))
                .Select(x => x.ProgrammeOrCourse!)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x
                })
                .ToListAsync();
        }
    }
}