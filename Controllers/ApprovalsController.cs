using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Facilitator")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Daily(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;

            var records = await _context.AttendanceRecords
                .Include(x => x.Student)
                .Where(x => x.AttendanceDate.Date == selectedDate)
                .OrderBy(x => x.Student.LastName)
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
                    CapturedImagePath = x.CapturedImagePath
                })
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;

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
                ApprovedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance approved successfully.";

            return RedirectToAction(nameof(Daily), new { date = record.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int attendanceRecordId, string? comment)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x => x.Id == attendanceRecordId);

            if (record == null)
                return NotFound();

            record.Status = "Rejected";

            _context.AttendanceApprovals.Add(new AttendanceApproval
            {
                AttendanceRecordId = record.Id,
                ApprovedByUserId = User.Identity?.Name ?? "System",
                IsApproved = false,
                Comment = string.IsNullOrWhiteSpace(comment) ? "Rejected" : comment,
                ApprovedAt = DateTime.Now
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
                .ThenInclude(x => x.Student)
                .OrderByDescending(x => x.ApprovedAt)
                .ToListAsync();

            return View(records);
        }
    }
}