using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class AttendanceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
