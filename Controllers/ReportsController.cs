using ClockItSystem.Data;
using ClockItSystem.Helpers;
using ClockItSystem.Models.Enums;
using ClockItSystem.Models.ViewModels;
using ClockItSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Project Manager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportService _reportService;

        public ReportsController(
            ApplicationDbContext context,
            ReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await BuildReportViewModel(new ReportFilterViewModel());

            return View("Report", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(ReportFilterViewModel filter)
        {
            var model = await BuildReportViewModel(filter);

            model.Results =
                await _reportService.ExecuteReportAsync(filter);

            return View("Report", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportExcel(ReportFilterViewModel filter)
        {
            var data =
                await _reportService.ExecuteReportAsync(filter);

            return await _reportService.ExportExcelAsync(
                data,
                filter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportPdf(ReportFilterViewModel filter)
        {
            var data =
                await _reportService.ExecuteReportAsync(filter);

            return await _reportService.ExportPdfAsync(
                data,
                filter);
        }

        private async Task<ReportViewModel> BuildReportViewModel(
            ReportFilterViewModel filter)
        {
            var model = new ReportViewModel
            {
                Filter = filter,
                FilterVisibility =
                    ReportFilterHelper.GetVisibility(filter.ReportType),

                Results = new List<ReportResultViewModel>()
            };

            model.Clients =
                await GetClients();

            model.Sites =
                await GetSites(filter.ClientId);

            model.Students =
                await GetStudents(
                    filter.ClientId,
                    filter.SiteId);

            model.Programmes =
                await GetProgrammes(
                    filter.ClientId,
                    filter.SiteId);

            model.ReportTypes =
                Enum.GetValues(typeof(ReportType))
                    .Cast<ReportType>()
                    .Select(x => new SelectListItem
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToString()
                    })
                    .ToList();

            return model;
        }


        [HttpGet]
        public async Task<IActionResult> GetFilterOptions(
            int? clientId,
            int? siteId)
        {
            var sites = await GetSites(clientId);

            var students = await GetStudents(
                clientId,
                siteId);

            var programmes = await GetProgrammes(
                clientId,
                siteId);

            return Json(new
            {
                sites,
                students,
                programmes
            });
        }

        private async Task<List<SelectListItem>> GetClients()
        {
            return await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientId.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSites(
            int? clientId)
        {
            return await _context.Sites
                .Where(s =>
                    s.IsActive &&
                    (!clientId.HasValue ||
                     s.ClientId == clientId))
                .OrderBy(s => s.SiteName)
                .Select(s => new SelectListItem
                {
                    Value = s.SiteId.ToString(),
                    Text = s.SiteName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetStudents(
            int? clientId,
            int? siteId)
        {
            return await _context.Students
                .Where(s =>
                    s.IsActive &&
                    (!clientId.HasValue ||
                     s.ClientId == clientId) &&
                    (!siteId.HasValue ||
                     s.SiteId == siteId))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.StudentNumber} - {s.FirstName} {s.LastName}"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetProgrammes(
            int? clientId,
            int? siteId)
        {
            return await _context.Students
                .Where(s =>
                    s.IsActive &&
                    (!clientId.HasValue ||
                     s.ClientId == clientId) &&
                    (!siteId.HasValue ||
                     s.SiteId == siteId) &&
                    !string.IsNullOrWhiteSpace(s.ProgrammeOrCourse))
                .Select(s => s.ProgrammeOrCourse!)
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new SelectListItem
                {
                    Value = p,
                    Text = p
                })
                .ToListAsync();
        }
    }
}