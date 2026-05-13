using ClockItSystem.Data;
using ClockItSystem.Models.ResultModels;
using ClockItSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly ApplicationDbContext _context;

        public FaceRecognitionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FaceMatchResult?> MatchFaceAsync(string imageBase64)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
            {
                return null;
            }

            var student = await _context.Students
                .Where(x => x.IsActive && !string.IsNullOrEmpty(x.FaceImagePath))
                .OrderBy(x => x.LastName)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return null;
            }

            return new FaceMatchResult
            {
                StudentId = student.Id,
                Score = 95.00m,
                Message = "Temporary face match successful."
            };
        }
    }
}