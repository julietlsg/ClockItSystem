using ClockItSystem.Models.ViewModels.Reports;

public interface IStudentService
{
    Task<StudentProfileReportViewModel?> GetStudentProfileAsync(int studentId);
}