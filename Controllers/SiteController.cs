using ClockItSystem.Helpers;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
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

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(PagedRequest request)
        {
            var model = await _siteService.GetAllAsync(request);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_SiteTable", model);
            }

            return View(model);
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var site = await _siteService.GetByIdAsync(id);

            if (site == null)
                return NotFound();

            return View(site);
        }

        #endregion

        #region Create

        public async Task<IActionResult> Create()
        {
            var model = new SiteViewModel
            {
                Clients = await _siteService.GetClientDropdownAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SiteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Clients = await _siteService.GetClientDropdownAsync();
                return View(model);
            }

            var result = await _siteService.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                model.Clients = await _siteService.GetClientDropdownAsync();
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var site = await _siteService.GetByIdAsync(id);

            if (site == null)
                return NotFound();

            var model = new SiteViewModel
            {
                SiteId = site.SiteId,
                ClientId = site.ClientId,
                SiteName = site.SiteName,
                SiteCode = site.SiteCode,
                Address = site.Address,
                IsActive = site.IsActive,
                Clients = await _siteService.GetClientDropdownAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SiteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Clients = await _siteService.GetClientDropdownAsync();
                return View(model);
            }

            var result = await _siteService.UpdateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                model.Clients = await _siteService.GetClientDropdownAsync();
                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Toggle Status

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _siteService.ToggleStatusAsync(id);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}