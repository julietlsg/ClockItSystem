using ClockItSystem.Data;
using ClockItSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Facilitator,Assessor")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Daily()
        {
            var today = DateTime.Today;

            var records = await _context.AttendanceRecords
                .Include(x => x.Student)
                .Where(x => x.AttendanceDate == today)
                .OrderBy(x => x.Student.LastName)
                .ToListAsync();

            return View(records);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int attendanceRecordId)
        {
            var record = await _context.AttendanceRecords.FindAsync(attendanceRecordId);

            if (record == null)
                return NotFound();

            record.Status = "Approved";

            _context.AttendanceApprovals.Add(new AttendanceApproval
            {
                AttendanceRecordId = record.Id,
                ApprovedByUserId = User.Identity?.Name ?? "Unknown",
                IsApproved = true
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Daily));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int attendanceRecordId, string? comment)
        {
            var record = await _context.AttendanceRecords.FindAsync(attendanceRecordId);

            if (record == null)
                return NotFound();

            record.Status = "Rejected";

            _context.AttendanceApprovals.Add(new AttendanceApproval
            {
                AttendanceRecordId = record.Id,
                ApprovedByUserId = User.Identity?.Name ?? "Unknown",
                IsApproved = false,
                Comment = comment
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Daily));
        }
    }
}
