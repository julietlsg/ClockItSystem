using ClockItSystem.Models.Requests;
using ClockItSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    [AllowAnonymous]
    public class BiometricController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IAttendanceService _attendanceService;
        private readonly IWebHostEnvironment _environment;

        public BiometricController(
            IFaceRecognitionService faceRecognitionService,
            IAttendanceService attendanceService,
            IWebHostEnvironment environment)
        {
            _faceRecognitionService = faceRecognitionService;
            _attendanceService = attendanceService;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Verify()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyFace([FromBody] FaceCaptureRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return Json(new
                {
                    success = false,
                    message = "No image was captured."
                });
            }

            var faceMatch = await _faceRecognitionService.MatchFaceAsync(request.ImageBase64);

            if (faceMatch == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No matching student found."
                });
            }

            var capturedImagePath = await SaveCapturedAttendanceImageAsync(request.ImageBase64);

            var attendanceRecordId = await _attendanceService.RecordAttendanceAsync(
                faceMatch.StudentId,
                "Face",
                faceMatch.Score,
                capturedImagePath);

            return Json(new
            {
                success = true,
                message = "Attendance recorded successfully and is pending facilitator approval.",
                studentId = faceMatch.StudentId,
                score = faceMatch.Score,
                attendanceRecordId = attendanceRecordId
            });
        }

        private async Task<string?> SaveCapturedAttendanceImageAsync(string imageBase64)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
                return null;

            var base64Data = imageBase64;

            if (imageBase64.Contains(","))
            {
                base64Data = imageBase64.Split(',')[1];
            }

            var imageBytes = Convert.FromBase64String(base64Data);

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "attendance");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}.png";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/uploads/attendance/{fileName}";
        }
    }
}