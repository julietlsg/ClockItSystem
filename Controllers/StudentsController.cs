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
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Client)
                .Include(s => s.Site)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            return View(students);
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.Client)
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            return View(student);
        }
        //[Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var model = new StudentViewModel
            {
                Clients = _context.Clients
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.ClientId.ToString(),
                        Text = c.Name
                    })
                    .ToList(),

                // IMPORTANT:
                // Do NOT load all sites.
                // They will be loaded after the user selects a Client.
                Sites = new List<SelectListItem>()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Clients = await GetClientsAsync();

                model.Sites = model.ClientId > 0
                    ? await _context.Sites
                        .Where(s => s.IsActive &&
                                    s.ClientId == model.ClientId)
                        .OrderBy(s => s.SiteName)
                        .Select(s => new SelectListItem
                        {
                            Value = s.SiteId.ToString(),
                            Text = s.SiteName
                        })
                        .ToListAsync()
                    : new List<SelectListItem>();

                return View(model);
            }

            var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.SiteId == model.SiteId);

            if (site == null || site.ClientId != model.ClientId)
            {
                ModelState.AddModelError(
                    nameof(model.SiteId),
                    "Selected site does not belong to the selected client.");

                model.Clients = await GetClientsAsync();

                model.Sites = await _context.Sites
                    .Where(s => s.IsActive &&
                                s.ClientId == model.ClientId)
                    .OrderBy(s => s.SiteName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SiteId.ToString(),
                        Text = s.SiteName
                    })
                    .ToListAsync();

                return View(model);
            }

            var imagePath = await SaveFaceImageAsync(model.FaceImage);

            var student = new Student
            {
                StudentNumber = model.StudentNumber,
                IdNumber = model.IdNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                ProgrammeOrCourse = model.ProgrammeOrCourse,
                ContactNumber = model.ContactNumber,
                FaceImagePath = imagePath,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                SiteId = model.SiteId,
                ClientId = model.ClientId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var model = new StudentViewModel
            {
                Id = student.Id,

                StudentNumber = student.StudentNumber,
                IdNumber = student.IdNumber,
                FirstName = student.FirstName,
                LastName = student.LastName,
                ProgrammeOrCourse = student.ProgrammeOrCourse,
                ContactNumber = student.ContactNumber,

                ExistingFaceImagePath = student.FaceImagePath,

                IsActive = student.IsActive,

                ClientId = student.ClientId,

                SiteId = student.SiteId,

                Clients = await _context.Clients
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.ClientId.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync(),

                // IMPORTANT:
                // Only load sites for THIS student's client.
                Sites = await _context.Sites
                    .Where(s => s.IsActive &&
                                s.ClientId == student.ClientId)
                    .OrderBy(s => s.SiteName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SiteId.ToString(),
                        Text = s.SiteName
                    })
                    .ToListAsync()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, StudentViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            var student = await _context.Students.FindAsync(id);


            if (student == null)
                return NotFound();

            var site = await _context.Sites
    .FirstOrDefaultAsync(s => s.SiteId == model.SiteId);

            if (site == null || site.ClientId != model.ClientId)
            {
                ModelState.AddModelError(
                    nameof(model.SiteId),
                    "Selected site does not belong to the selected client.");

                model.Clients = await GetClientsAsync();

                model.Sites = await _context.Sites
                    .Where(s => s.IsActive &&
                                s.ClientId == model.ClientId)
                    .OrderBy(s => s.SiteName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SiteId.ToString(),
                        Text = s.SiteName
                    })
                    .ToListAsync();

                return View(model);
            }

            student.StudentNumber = model.StudentNumber;
            student.IdNumber = model.IdNumber;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.ProgrammeOrCourse = model.ProgrammeOrCourse;
            student.ContactNumber = model.ContactNumber;
            student.IsActive = model.IsActive;
            student.ClientId = model.ClientId;
            student.SiteId = model.SiteId;


            if (model.FaceImage != null)
            {
                student.FaceImagePath = await SaveFaceImageAsync(model.FaceImage);
            }

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            student.IsActive = false;

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> SaveFaceImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Only JPG, JPEG and PNG files are allowed.");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "students");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/students/{fileName}";
        }

        [HttpGet]
        public async Task<IActionResult> GetSitesByClient(int clientId)
        {
            var sites = await _context.Sites
                .Where(s => s.ClientId == clientId && s.IsActive)
                .OrderBy(s => s.SiteName)
                .Select(s => new
                {
                    value = s.SiteId,
                    text = s.SiteName
                })
                .ToListAsync();

            return Json(sites);
        }

        private async Task<List<SelectListItem>> GetClientsAsync()
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

        private async Task<List<SelectListItem>> GetSitesAsync()
        {
            return await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.SiteName)
                .Select(s => new SelectListItem
                {
                    Value = s.SiteId.ToString(),
                    Text = s.SiteName
                })
                .ToListAsync();
        }
    }
}