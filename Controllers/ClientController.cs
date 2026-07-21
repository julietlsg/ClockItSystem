using ClockItSystem.Helpers;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClockItSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(PagedRequest request)
        {
            var model = await _clientService.GetAllAsync(request);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ClientTable", model);
            }

            return View(model);
        }        
        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client == null)
                return NotFound();

            return View(client);
        }

        #endregion

        #region Create

        public IActionResult Create()
        {
            return View(new ClientViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            ServiceResult result =
                await _clientService.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                return View(model);
            }

            TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var client = await _clientService.GetByIdAsync(id);

            if (client == null)
                return NotFound();

            var model = new ClientViewModel
            {
                ClientId = client.ClientId,
                Name = client.Name,
                Code = client.Code,
                ContactPerson = client.ContactPerson,
                Email = client.Email,
                Phone = client.Phone,
                Address = client.Address,
                IsActive = client.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            ServiceResult result =
                await _clientService.UpdateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

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
            ServiceResult result =
                await _clientService.ToggleStatusAsync(id);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}