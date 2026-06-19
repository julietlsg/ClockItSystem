using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers.Api
{
    [ApiController]
    [Route("api/fingerprint")]
    public class FingerprintApiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
