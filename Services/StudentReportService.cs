using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class StudentReportService
    {
        private readonly ApplicationDbContext _context;

        public StudentReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a student's profile report.
        /// </summary>
        /// <param name="studentId">Student Id</param>
        /// <returns>Student Profile Report</returns>
        public async Task<StudentProfileReportViewModel?> GetStudentProfileAsync(int studentId)
        {
            return (await _context.Set<StudentProfileReportViewModel>()
                .FromSqlInterpolated($@"
            EXEC dbo.usp_GetStudentProfile
                @StudentId = {studentId}")
                .AsNoTracking()
                .ToListAsync())
                .FirstOrDefault();
        }
        /// <summary>
        /// Retrieves all student profile reports matching the supplied filter.
        /// This can later support batch reports or exports.
        /// </summary>
        public async Task<List<StudentProfileReportViewModel>> GetStudentProfilesAsync(
            ReportFilterViewModel filter)
        {
            IQueryable<Student> query = _context.Students
                .Where(s => s.IsActive);

            if (filter.ClientId.HasValue)
                query = query.Where(s => s.ClientId == filter.ClientId);

            if (filter.SiteId.HasValue)
                query = query.Where(s => s.SiteId == filter.SiteId);

            if (filter.StudentId.HasValue)
                query = query.Where(s => s.Id == filter.StudentId);

            var studentIds = await query
                .Select(s => s.Id)
                .ToListAsync();

            var reports = new List<StudentProfileReportViewModel>();

            foreach (var studentId in studentIds)
            {
                var report = await GetStudentProfileAsync(studentId);

                if (report != null)
                    reports.Add(report);
            }

            return reports;
        }
    }
}