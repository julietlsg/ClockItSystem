using ClockItSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class SiteController : Controller
    {
         private readonly ISiteService _siteService;

        public SiteController(ISiteService siteService)
        {
            _siteService = siteService;
        }

        public async Task<IActionResult> Index()
        {
            var sites = await _siteService.GetAllAsync();

            return View(sites);
        }
    }
}
