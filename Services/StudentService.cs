using ClockItSystem.Data;
using ClockItSystem.Models.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;

    public StudentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentProfileReportViewModel?> GetStudentProfileAsync(int studentId)
    {
        var results = await _context.StudentProfiles
            .FromSqlInterpolated($"EXEC dbo.usp_GetStudentProfile @StudentId={studentId}")
            .AsNoTracking()
            .ToListAsync();

        return results.FirstOrDefault();
    }
}