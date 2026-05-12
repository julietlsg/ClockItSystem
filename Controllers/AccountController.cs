using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
