namespace ClockItSystem.Interfaces
{
    public interface IAttendanceService
    {
        Task<int> RecordAttendanceAsync(int studentId, string method, decimal score, string? capturedImagePath);

    }
}
