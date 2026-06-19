using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers.Api
{
    public class FaceApiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
