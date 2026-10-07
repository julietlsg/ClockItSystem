using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StipendRatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StipendRatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /StipendRates
        public async Task<IActionResult> Index()
        {
            var rates = await _context.ClientStipendRates
                .AsNoTracking()
                .Include(x => x.Client)
                .OrderBy(x => x.Client.Name)
                .ThenByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return View(rates);
        }

        // GET: /StipendRates/Create
        public async Task<IActionResult> Create()
        {
            var model = new ClientStipendRateViewModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            };

            await PopulateClients(model);

            return View(model);
        }

        // POST: /StipendRates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientStipendRateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClients(model);
                return View(model);
            }

            // Make sure the selected client exists and is active.
            var client = await _context.Clients
                .FirstOrDefaultAsync(c =>
                    c.ClientId == model.ClientId &&
                    c.IsActive);

            if (client == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "The selected client does not exist or is inactive.");

                await PopulateClients(model);
                return View(model);
            }

            // Effective To cannot be before Effective From.
            if (model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EffectiveTo),
                    "Effective To cannot be before Effective From.");

                await PopulateClients(model);
                return View(model);
            }

            // Prevent an active rate from overlapping another active rate
            // for the same client.
            if (model.IsActive)
            {
                var hasOverlappingRate = await _context.ClientStipendRates
                    .AnyAsync(x =>
                        x.ClientId == model.ClientId &&
                        x.IsActive &&
                        (
                            // New rate starts inside an existing rate
                            model.EffectiveFrom.Date >= x.EffectiveFrom.Date &&
                            (
                                !x.EffectiveTo.HasValue ||
                                model.EffectiveFrom.Date <= x.EffectiveTo.Value.Date
                            )
                            ||
                            // New rate ends inside an existing rate
                            model.EffectiveTo.HasValue &&
                            model.EffectiveTo.Value.Date >= x.EffectiveFrom.Date &&
                            (
                                !x.EffectiveTo.HasValue ||
                                model.EffectiveTo.Value.Date <= x.EffectiveTo.Value.Date
                            )
                        ));

                if (hasOverlappingRate)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "An active stipend rate already exists for this client during the selected period.");

                    await PopulateClients(model);
                    return View(model);
                }
            }

            var rate = new ClientStipendRate
            {
                ClientId = model.ClientId,
                DailyRate = model.DailyRate,
                EffectiveFrom = model.EffectiveFrom.Date,
                EffectiveTo = model.EffectiveTo?.Date,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            _context.ClientStipendRates.Add(rate);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Stipend rate of R{rate.DailyRate:N2} per day was added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /StipendRates/Deactivate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var rate = await _context.ClientStipendRates
                .FirstOrDefaultAsync(x => x.Id == id);

            if (rate == null)
                return NotFound();

            rate.IsActive = false;

            // If the rate has no end date, set it to today.
            if (!rate.EffectiveTo.HasValue)
            {
                rate.EffectiveTo = DateTime.Today;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "The stipend rate has been deactivated.";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateClients(ClientStipendRateViewModel model)
        {
            model.Clients = await _context.Clients
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientId.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}