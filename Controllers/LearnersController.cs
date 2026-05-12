using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class LearnersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
