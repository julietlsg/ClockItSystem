using ClockItSystem.Data;
using ClockItSystem.Models.ResultModels;
using ClockItSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClockItSystem.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly ApplicationDbContext _context;

        // Temporary calibration value.
        // We will reduce this after testing real same-student distances.
        private const double MaximumAllowedDistance = 0.55;

        public FaceRecognitionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FaceMatchResult?> MatchFaceAsync(string descriptorJson)
        {
            if (string.IsNullOrWhiteSpace(descriptorJson))
                return null;

            var capturedDescriptor = JsonSerializer.Deserialize<double[]>(descriptorJson);

            if (capturedDescriptor == null || capturedDescriptor.Length == 0)
                return null;

            var enrolledProfiles = await _context.BiometricProfiles
                .Include(x => x.Student)
                .Where(x =>
                    x.BiometricType == "Face" &&
                    x.IsVerified &&
                    !string.IsNullOrEmpty(x.BiometricTemplate) &&
                    x.Student.IsActive)
                .ToListAsync();

            if (!enrolledProfiles.Any())
                return null;

            int? bestStudentId = null;
            double bestDistance = double.MaxValue;

            foreach (var profile in enrolledProfiles)
            {
                var storedDescriptor = JsonSerializer.Deserialize<double[]>(profile.BiometricTemplate!);

                if (storedDescriptor == null)
                    continue;

                if (storedDescriptor.Length != capturedDescriptor.Length)
                    continue;

                var distance = CalculateEuclideanDistance(capturedDescriptor, storedDescriptor);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestStudentId = profile.StudentId;
                }
            }

            if (bestStudentId == null)
                return null;

            if (bestDistance > MaximumAllowedDistance)
                return null;

            var score = ConvertDistanceToScore(bestDistance);

            return new FaceMatchResult
            {
                StudentId = bestStudentId.Value,
                Score = score,
                Message = $"Face matched. Distance: {bestDistance:F4}"
            };
        }

        private static double CalculateEuclideanDistance(double[] first, double[] second)
        {
            double sum = 0;

            for (int i = 0; i < first.Length; i++)
            {
                var difference = first[i] - second[i];
                sum += difference * difference;
            }

            return Math.Sqrt(sum);
        }

        private static decimal ConvertDistanceToScore(double distance)
        {
            var score = Math.Max(0, (1.0 - distance) * 100);
            return Math.Round((decimal)score, 2);
        }
    }
}