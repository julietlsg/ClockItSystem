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

        public async Task<int> RecordAttendanceAsync(
            int studentId,
            string method,
            decimal score,
            string? capturedImagePath)
        {
            var today = DateTime.Today;

            var existingRecord = await _context.AttendanceRecords
                .FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.AttendanceDate == today);

            if (existingRecord != null)
            {
                return existingRecord.Id;
            }

            var attendanceRecord = new AttendanceRecord
            {
                StudentId = studentId,
                AttendanceDate = today,
                ClockTime = DateTime.Now,
                VerificationMethod = method,
                VerificationScore = score,
                CapturedImagePath = capturedImagePath,
                Status = "PendingApproval"
            };

            _context.AttendanceRecords.Add(attendanceRecord);

            await _context.SaveChangesAsync();

            return attendanceRecord.Id;
        }
    }
}
