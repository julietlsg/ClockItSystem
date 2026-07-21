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

            // Build the attendance register
            var query = BuildDailyRegisterQuery(selectedDate);

            // Apply user-selected filters
            query = ApplyFilters(
                query,
                clientId,
                siteId,
                programme,
                searchTerm);


            // Get total records for paging
            var totalRecords = await query.CountAsync();

            // Retrieve current page
            var records = await query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate dropdowns and paging information
            await PopulateDropdowns(
                records,
                clientId,
                siteId,
                programme,
                searchTerm,
                selectedDate,
                page,
                pageSize,
                totalRecords);

            // Ensure filters remain visible even when no students match
            if (!records.Any())
            {
                var emptyRecord = new DailyApprovalViewModel();

                await PopulateDropdowns(
                    new List<DailyApprovalViewModel> { emptyRecord },
                    clientId,
                    siteId,
                    programme,
                    searchTerm,
                    selectedDate,
                    page,
                    pageSize,
                    totalRecords);

                records.Add(emptyRecord);
            }

            return View(records);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int studentId,
            DateTime attendanceDate)
        {
            attendanceDate = attendanceDate.Date;

            var student = await _context.Students
                .Include(x => x.Client)
                .Include(x => x.Site)
                .FirstOrDefaultAsync(x => x.Id == studentId);

            if (student == null)
                return NotFound();

            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.AttendanceDate.Date == attendanceDate);

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    StudentId = student.Id,

                    AttendanceDate = attendanceDate,

                    ClockTime = DateTime.Now,

                    VerificationMethod = "Manual",

                    VerificationScore = null,

                    Status = "Approved",

                    ClientId = student.ClientId,

                    SiteId = student.SiteId,

                    CreatedByUserId = User.Identity?.Name
                };

                _context.AttendanceRecords.Add(record);

                await _context.SaveChangesAsync();
            }
            else
            {
                record.Status = "Approved";
            }

            var existingApproval = await _context.AttendanceApprovals
                .FirstOrDefaultAsync(x =>
                    x.AttendanceRecordId == record.Id);

            if (existingApproval == null)
            {
                _context.AttendanceApprovals.Add(new AttendanceApproval
                {
                    AttendanceRecordId = record.Id,

                    ApprovedByUserId = User.Identity?.Name ?? "System",

                    IsApproved = true,

                    Comment = "Approved",

                    ApprovedAt = DateTime.Now,

                    ClientId = record.ClientId,

                    SiteId = record.SiteId
                });
            }
            else
            {
                existingApproval.IsApproved = true;

                existingApproval.Comment = "Approved";

                existingApproval.ApprovedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance approved successfully.";

            return RedirectToAction(nameof(Daily),
                new
                {
                    date = attendanceDate.ToString("yyyy-MM-dd")
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int studentId,
            DateTime attendanceDate,
            string reason,
            string? comment,
            IFormFile? supportingDocument)
        {
            attendanceDate = attendanceDate.Date;

            var student = await _context.Students
                .Include(s => s.Client)
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            var attendanceRecord = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.AttendanceDate.Date == attendanceDate);

            if (attendanceRecord == null)
            {
                attendanceRecord = new AttendanceRecord
                {
                    StudentId = student.Id,

                    AttendanceDate = attendanceDate,

                    ClockTime = DateTime.Now,

                    VerificationMethod = "Manual",

                    VerificationScore = null,

                    Status = "Rejected",

                    ClientId = student.ClientId,

                    SiteId = student.SiteId,

                    CreatedByUserId = User.Identity?.Name
                };

                _context.AttendanceRecords.Add(attendanceRecord);

                await _context.SaveChangesAsync();
            }
            else
            {
                attendanceRecord.Status = "Rejected";
            }

            if (!Enum.TryParse<AttendanceRejectionReason>(
                    reason,
                    true,
                    out var rejectionReason))
            {
                TempData["Error"] = "Please select a valid reason.";

                return RedirectToAction(nameof(Daily),
                    new
                    {
                        date = attendanceDate.ToString("yyyy-MM-dd")
                    });
            }

            var documentPath =
                await _fileStorageService
                    .SaveAttendanceDocumentAsync(supportingDocument);

            var approval = await _context.AttendanceApprovals
                .FirstOrDefaultAsync(x =>
                    x.AttendanceRecordId == attendanceRecord.Id);

            if (approval == null)
            {
                approval = new AttendanceApproval
                {
                    AttendanceRecordId = attendanceRecord.Id,

                    ApprovedByUserId =
                        User.Identity?.Name ?? "System",

                    IsApproved = false,

                    ApprovedAt = DateTime.Now,

                    ClientId = attendanceRecord.ClientId,

                    SiteId = attendanceRecord.SiteId
                };

                _context.AttendanceApprovals.Add(approval);
            }

            approval.IsApproved = false;

            approval.Comment =
                string.IsNullOrWhiteSpace(comment)
                    ? "Rejected"
                    : comment;

            approval.Reason = rejectionReason;

            approval.SupportingDocumentPath = documentPath;

            approval.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance updated successfully.";

            return RedirectToAction(nameof(Daily),
                new
                {
                    date = attendanceDate.ToString("yyyy-MM-dd")
                });
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

        private IQueryable<DailyApprovalViewModel> BuildDailyRegisterQuery(DateTime selectedDate)
        {
            return
                from student in _context.Students

                where student.IsActive

                join attendance in _context.AttendanceRecords
                    .Where(a => a.AttendanceDate.Date == selectedDate)

                    on student.Id equals attendance.StudentId
                    into attendanceGroup

                from attendance in attendanceGroup.DefaultIfEmpty()

                select new DailyApprovalViewModel
                {
                    AttendanceRecordId =
                        attendance != null
                            ? attendance.Id
                            : 0,

                    StudentId = student.Id,

                    StudentNumber = student.StudentNumber,
                    FirstName = student.FirstName,

                    LastName = student.LastName,

                    StudentName = student.FirstName + " " + student.LastName,

                    //StudentName =
                    //    $"{student.FirstName} {student.LastName}",

                    ProgrammeOrCourse =
                        student.ProgrammeOrCourse,

                    AttendanceDate =
                        attendance != null
                            ? attendance.AttendanceDate
                            : selectedDate,

                    ClockTime =
                        attendance != null
                            ? attendance.ClockTime
                            : DateTime.MinValue,

                    VerificationMethod =
                        attendance != null
                            ? attendance.VerificationMethod
                            : "Not Verified",

                    VerificationScore =
                        attendance != null
                            ? attendance.VerificationScore
                            : null,

                    CapturedImagePath =
                        attendance != null
                            ? attendance.CapturedImagePath
                            : null,

                    Status =
                        attendance != null
                            ? attendance.Status
                            : "Not Verified",

                    ClientId = student.ClientId,

                    SiteId = student.SiteId,

                    ClientName =
                        student.Client != null
                            ? student.Client.Name
                            : string.Empty,

                    SiteName =
                        student.Site != null
                            ? student.Site.SiteName
                            : string.Empty
                };
        }
        private IQueryable<DailyApprovalViewModel> ApplyFilters(
            IQueryable<DailyApprovalViewModel> query,
            int? clientId,
            int? siteId,
            string? programme,
            string? searchTerm)
                {
                    if (clientId.HasValue)
                    {
                        query = query.Where(x =>
                            x.ClientId == clientId.Value);
                    }

                    if (siteId.HasValue)
                    {
                        query = query.Where(x =>
                            x.SiteId == siteId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(programme))
                    {
                        query = query.Where(x =>
                            x.ProgrammeOrCourse == programme);
                    }

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        searchTerm = searchTerm.Trim();

                        query = query.Where(x =>

                            x.StudentNumber.Contains(searchTerm)

                            ||

                            x.StudentName.Contains(searchTerm)

                            ||

                            (x.ProgrammeOrCourse != null &&
                             x.ProgrammeOrCourse.Contains(searchTerm)));
                    }

                    return query;
                }

        private async Task PopulateDropdowns(
            List<DailyApprovalViewModel> records,
            int? clientId,
            int? siteId,
            string? programme,
            string? searchTerm,
            DateTime selectedDate,
            int page,
            int pageSize,
            int totalRecords)
        {
            var clients = await GetClientsAsync();

            var sites = await GetSitesAsync(clientId);

            var programmes = await GetProgrammesAsync();

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


        [HttpGet]
        public async Task<IActionResult> GetSitesByClient(int? clientId)
        {
            var query = _context.Sites
                .Where(x => x.IsActive);

            if (clientId.HasValue)
            {
                query = query.Where(x => x.ClientId == clientId.Value);
            }

            var sites = await query
                .OrderBy(x => x.SiteName)
                .Select(x => new
                {
                    value = x.SiteId,
                    text = x.SiteName
                })
                .ToListAsync();

            return Json(sites);
        }
    }
}