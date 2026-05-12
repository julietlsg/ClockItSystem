using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RecordAttendanceAsync(int studentId, string method, decimal score, string? imagePath)
        {
            var today = DateTime.Today;

            var existing = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x => x.StudentId == studentId &&
                                          x.AttendanceDate == today);

            if (existing != null)
                return;

            var record = new AttendanceRecord
            {
                StudentId = studentId,
                AttendanceDate = today,
                ClockTime = DateTime.Now,
                VerificationMethod = method,
                VerificationScore = score,
                CapturedImagePath = imagePath,
                Status = "PendingApproval"
            };

            _context.AttendanceRecords.Add(record);
            await _context.SaveChangesAsync();
        }
    }
}
