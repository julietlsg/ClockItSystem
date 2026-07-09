using ClockItSystem.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ClockItSystem.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public FileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SaveAttendanceDocumentAsync(IFormFile? file)
        {
            return await SaveFileAsync(file, "uploads/attendance");
        }

        public async Task<string?> SaveStudentFaceImageAsync(IFormFile? file)
        {
            return await SaveFileAsync(file, "uploads/faces");
        }

        public async Task DeleteFileAsync(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            await Task.CompletedTask;
        }

        public bool FileExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(fullPath);
        }

        private async Task<string?> SaveFileAsync(
            IFormFile? file,
            string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                folder.Replace('/', Path.DirectorySeparatorChar));

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(file.FileName);

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var fullPath = Path.Combine(
                uploadsFolder,
                fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/" + folder + "/" + fileName;
        }
    }
}