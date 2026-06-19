using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.Requests;
using ClockItSystem.Services.Api;
using ClockItSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [AllowAnonymous]
    public class BiometricController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IAttendanceService _attendanceService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly BiometricApiClient _biometricApi;

        public BiometricController(
            IFaceRecognitionService faceRecognitionService,
            IAttendanceService attendanceService,
            IWebHostEnvironment environment,
            ApplicationDbContext context,
            BiometricApiClient biometricApiClient)
        {
            _faceRecognitionService = faceRecognitionService;
            _attendanceService = attendanceService;
            _environment = environment;
            _context = context;
            _biometricApi = biometricApiClient;
        }

        public async Task<IActionResult> Index(int studentId)
        {
            var student = await _context.Students
                .Include(x => x.BiometricProfiles)
                .FirstOrDefaultAsync(x => x.Id == studentId);

            if (student == null)
                return NotFound();

            return View(student);
        }

        [HttpGet]
        public IActionResult Verify()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyFace([FromBody] FaceCaptureRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.ImageBase64) ||
                string.IsNullOrWhiteSpace(request.DescriptorJson))
            {
                return Json(new
                {
                    success = false,
                    message = "No valid face data was captured."
                });
            }

            var faceMatch = await _faceRecognitionService.MatchFaceAsync(request.DescriptorJson);

            if (faceMatch == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Face not recognised. Please enrol the student before attendance verification."
                });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(x => x.Id == faceMatch.StudentId);

            if (student == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Matched student record could not be found."
                });
            }

            var capturedImagePath = await SaveCapturedAttendanceImageAsync(request.ImageBase64);

            var apiSuccess =
    await _biometricApi.EnrollFaceAsync(
        request.StudentId,
        request.DescriptorJson);

            var attendanceRecordId = await _attendanceService.RecordAttendanceAsync(
                faceMatch.StudentId,
                "Face",
                faceMatch.Score,
                capturedImagePath);

            return Json(new
            {
                success = true,
                message = "Attendance Recorded",
                studentId = student.Id,
                studentName = $"{student.FirstName} {student.LastName}",
                studentNumber = student.StudentNumber,
                attendanceRecordId
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> Enroll(int studentId)
        //{
        //    var student = await _context.Students.FindAsync(studentId);

        //    if (student == null)
        //        return NotFound();

        //    ViewBag.StudentId = student.Id;
        //    ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

        //    return View();
        //}

        [HttpGet]
        public async Task<IActionResult> Enroll(int studentId)
        {
            var student = await _context.Students
                .Include(x => x.BiometricProfiles)
                .FirstOrDefaultAsync(x => x.Id == studentId);

            if (student == null)
                return NotFound();

            ViewBag.StudentId = student.Id;
            ViewBag.StudentName = $"{student.FirstName} {student.LastName}";

            ViewBag.FaceEnrolled =
                student.BiometricProfiles.Any(x =>
                    x.BiometricType == "Face" &&
                    x.IsVerified);

            ViewBag.FingerprintEnrolled =
                student.BiometricProfiles.Any(x =>
                    x.BiometricType == "Fingerprint" &&
                    x.IsVerified);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnrollFace([FromBody] FaceEnrollmentRequest request)
        {
            if (request == null ||
                request.StudentId <= 0 ||
                string.IsNullOrWhiteSpace(request.ImageBase64) ||
                string.IsNullOrWhiteSpace(request.DescriptorJson))
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid face enrolment data."
                });
            }

            var student = await _context.Students.FindAsync(request.StudentId);

            if (student == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Student not found."
                });
            }

            var imagePath = await SaveCapturedAttendanceImageAsync(request.ImageBase64);

            var existingProfile = await _context.BiometricProfiles
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.BiometricType == "Face");

            if (existingProfile == null)
            {
                var profile = new BiometricProfile
                {
                    StudentId = request.StudentId,
                    BiometricType = "Face",
                    FaceImagePath = imagePath,
                    BiometricTemplate = request.DescriptorJson,
                    IsVerified = true,
                    EnrolledAt = DateTime.Now
                };

                _context.BiometricProfiles.Add(profile);
            }
            else
            {
                existingProfile.FaceImagePath = imagePath;
                existingProfile.BiometricTemplate = request.DescriptorJson;
                existingProfile.IsVerified = true;
                existingProfile.EnrolledAt = DateTime.Now;

                _context.BiometricProfiles.Update(existingProfile);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Face enrolled successfully."
            });
        }

        [HttpGet]
        public async Task<IActionResult> EnrollFingerprint(int studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(x => x.Id == studentId);

            if (student == null)
                return NotFound();

            ViewBag.StudentId = student.Id;
            ViewBag.StudentName =
                $"{student.FirstName} {student.LastName}";

            return View();
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