using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
