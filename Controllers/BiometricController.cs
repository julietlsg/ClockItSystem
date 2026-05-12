using ClockItSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    [Authorize]
    public class BiometricController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;

        public BiometricController(IFaceRecognitionService faceRecognitionService)
        {
            _faceRecognitionService = faceRecognitionService;
        }

        [HttpGet]
        public IActionResult Verify()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyFace([FromBody] FaceCaptureRequest request)
        {
            var result = await _faceRecognitionService.MatchFaceAsync(request.ImageBase64);

            if (result == null)
            {
                return Json(new { success = false, message = "No matching student found." });
            }

            return Json(new
            {
                success = true,
                studentId = result.StudentId,
                score = result.Score
            });
        }
    }

    public class FaceCaptureRequest
    {
        public string ImageBase64 { get; set; } = string.Empty;
    }
}
