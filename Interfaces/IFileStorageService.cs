using Microsoft.AspNetCore.Http;

namespace ClockItSystem.Interfaces
{
    public interface IFileStorageService
    {
        Task<string?> SaveAttendanceDocumentAsync(IFormFile? file);

        Task<string?> SaveStudentFaceImageAsync(IFormFile? file);

        Task DeleteFileAsync(string? relativePath);

        bool FileExists(string? relativePath);
    }
}