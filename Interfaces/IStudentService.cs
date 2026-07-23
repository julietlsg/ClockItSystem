using ClockItSystem.Models.ViewModels;

public interface IStudentService
{
    Task<StudentProfileViewModel?> GetStudentProfileAsync(int studentId);
}