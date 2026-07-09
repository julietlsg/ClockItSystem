using ClockItSystem.Models.ResultModels;

namespace ClockItSystem.Interfaces
{
    public interface IFaceRecognitionService
    {
        //Task<string> EnrollFaceAsync(int studentId, string imageBase64);
        Task<FaceMatchResult?> MatchFaceAsync(string descriptorJson);
    }
}
