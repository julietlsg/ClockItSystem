using ClockItSystem.Models.ResultModels;

namespace ClockItSystem.Services.Interfaces
{
    public interface IFaceRecognitionService
    {
        Task<string> EnrollFaceAsync(int learnerId, string imageBase64);
        Task<FaceMatchResult?> MatchFaceAsync(string capturedImageBase64);
    }
}
