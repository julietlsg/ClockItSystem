using ClockItSystem.Data;
using ClockItSystem.Interfaces;
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
        private readonly IPersonValidationService _personValidation;
        private readonly IStudentService _studentService;

        public StudentsController(ApplicationDbContext context, 
            IWebHostEnvironment environment,
            IStudentService studentService,
            IPersonValidationService validationService)
        {
            _context = context;
            _environment = environment;
            _personValidation = validationService;
            _studentService = studentService;
        }

        public async Task<IActionResult> Index(StudentSearchViewModel model)
        {
            var query = _context.Students
             .AsNoTracking()
             .Include(s => s.Client)
             .Include(s => s.Site)
             .AsQueryable();

            // Global Search
            if (!string.IsNullOrWhiteSpace(model.SearchTerm))
            {
                string search = model.SearchTerm.Trim();

                query = query.Where(s =>

                    s.StudentNumber.Contains(search) ||

                    s.FirstName.Contains(search) ||

                    s.LastName.Contains(search) ||

                    s.IdNumber.Contains(search) ||

                    s.ContactNumber.Contains(search));
            }

            // Client Filter
            if (model.ClientId.HasValue)
            {
                query = query.Where(s =>
                    s.ClientId == model.ClientId.Value);
            }

            // Site Filter
            if (model.SiteId.HasValue)
            {
                query = query.Where(s =>
                    s.SiteId == model.SiteId.Value);
            }

            // Programme Filter
            if (!string.IsNullOrWhiteSpace(model.ProgrammeOrCourse))
            {
                query = query.Where(s =>
                    s.ProgrammeOrCourse == model.ProgrammeOrCourse);
            }


            // Active Filter
            if (model.IsActive.HasValue)
            {
                query = query.Where(s =>
                    s.IsActive == model.IsActive.Value);
            }

            model.Clients = await GetClientsAsync();

            model.Sites = model.ClientId.HasValue
                ? await _context.Sites
                    .Where(s => s.IsActive &&
                                s.ClientId == model.ClientId.Value)
                    .OrderBy(s => s.SiteName)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SiteId.ToString(),
                        Text = s.SiteName
                    })
                    .ToListAsync()
                : new List<SelectListItem>();

            model.Programmes = await _context.Students
                .Where(s => !string.IsNullOrWhiteSpace(s.ProgrammeOrCourse))
                .Select(s => s.ProgrammeOrCourse!)
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new SelectListItem
                {
                    Text = p,
                    Value = p
                })
                .ToListAsync();

            var students = await query
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            model.Students = students
                .Select(MapStudentToViewModel)
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.Client)
                .Include(s => s.Site)
                .Include(s => s.Bank)
                .Include(s => s.BankBranch)
                .Include(s => s.AccountType)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var model = new StudentProfileViewModel
            {
                StudentId = student.Id,

                StudentNumber = student.StudentNumber,
                FirstName = student.FirstName,
                LastName = student.LastName,
                FullName = $"{student.FirstName} {student.LastName}",

                IdNumber = student.IdNumber,
                ContactNumber = student.ContactNumber,
                ProgrammeOrCourse = student.ProgrammeOrCourse,

                IsActive = student.IsActive,
                CreatedAt = student.CreatedAt,

                ClientId = student.ClientId,
                ClientName = student.Client?.Name ?? string.Empty,

                SiteId = student.SiteId,
                SiteName = student.Site?.SiteName ?? string.Empty,

                BankId = student.BankId,
                BankName = student.Bank?.BankName,

                BankBranchId = student.BankBranchId,
                BranchName = student.BankBranch?.BranchName,

                AccountTypeId = student.AccountTypeId,
                AccountTypeName = student.AccountType?.AccountTypeName,

                AccountHolderName = student.AccountHolderName,
                AccountNumber = student.AccountNumber,

                FaceImagePath = student.FaceImagePath
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var model = new StudentViewModel();

            await PopulateDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (!_personValidation.IsValidSouthAfricanId(model.IdNumber))
            {
                ModelState.AddModelError(nameof(model.IdNumber),
                    "Please enter a valid 13-digit South African ID number.");
            }

            if (!_personValidation.IsValidCellphone(model.ContactNumber))
            {
                ModelState.AddModelError(nameof(model.ContactNumber),
                    "Please enter a valid South African cellphone number.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);

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
            if (CanEditBankingDetails())
            {
                student.BankId = model.BankId > 0 ? model.BankId : null;

                student.BankBranchId = model.BankBranchId > 0
                    ? model.BankBranchId
                    : null;

                student.AccountTypeId = model.AccountTypeId > 0
                    ? model.AccountTypeId
                    : null;

                student.AccountHolderName =
                    string.IsNullOrWhiteSpace(model.AccountHolderName)
                        ? null
                        : model.AccountHolderName.Trim();

                student.AccountNumber =
                    string.IsNullOrWhiteSpace(model.AccountNumber)
                        ? null
                        : model.AccountNumber.Trim();
            }
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

            var model = MapStudentToViewModel(student);

            await PopulateDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var student = await _context.Students.FindAsync(id);


            if (student == null)
                return NotFound();

            if (!_personValidation.IsValidSouthAfricanId(model.IdNumber))
            {
                ModelState.AddModelError(nameof(model.IdNumber),
                    "Please enter a valid 13-digit South African ID number.");
            }

            if (!_personValidation.IsValidCellphone(model.ContactNumber))
            {
                ModelState.AddModelError(nameof(model.ContactNumber),
                    "Please enter a valid South African cellphone number.");
            }

            var site = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == model.SiteId);

            student.StudentNumber = model.StudentNumber;
            student.IdNumber = model.IdNumber;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.ProgrammeOrCourse = model.ProgrammeOrCourse;
            student.ContactNumber = model.ContactNumber;
            student.IsActive = model.IsActive;
            student.ClientId = model.ClientId;
            student.SiteId = model.SiteId;
            if (CanEditBankingDetails())
            {
                student.BankId = model.BankId > 0 ? model.BankId : null;

                student.BankBranchId = model.BankBranchId > 0
                    ? model.BankBranchId
                    : null;

                student.AccountTypeId = model.AccountTypeId > 0
                    ? model.AccountTypeId
                    : null;

                student.AccountHolderName =
                    string.IsNullOrWhiteSpace(model.AccountHolderName)
                        ? null
                        : model.AccountHolderName.Trim();

                student.AccountNumber =
                    string.IsNullOrWhiteSpace(model.AccountNumber)
                        ? null
                        : model.AccountNumber.Trim();
            }

            if (model.FaceImage != null)
            {
                student.FaceImagePath = await SaveFaceImageAsync(model.FaceImage);
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] = "Student updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);

                await PopulateDropdowns(model);

                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> GetBankBranches(int bankId)
        {
            var branches = await _context.BankBranches
                .Where(b => b.BankId == bankId && b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new
                {
                    value = b.BankBranchId,
                    text = $"{b.BranchName} ({b.BranchCode})"
                })
                .ToListAsync();

            return Json(branches);
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

        private async Task<List<SelectListItem>> GetBanksAsync()
        {
            return await _context.Banks
                .Where(b => b.IsActive)
                .OrderBy(b => b.BankName)
                .Select(b => new SelectListItem
                {
                    Value = b.BankId.ToString(),
                    Text = b.BankName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetAccountTypesAsync()
        {
            return await _context.AccountTypes
                .Where(a => a.IsActive)
                .OrderBy(a => a.AccountTypeName)
                .Select(a => new SelectListItem
                {
                    Value = a.AccountTypeId.ToString(),
                    Text = a.AccountTypeName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetBankBranchesAsync(int bankId)
        {
            return await _context.BankBranches
                .Where(b => b.IsActive && b.BankId == bankId)
                .OrderBy(b => b.BranchName)
                .Select(b => new SelectListItem
                {
                    Value = b.BankBranchId.ToString(),
                    Text = $"{b.BranchName} ({b.BranchCode})"
                })
                .ToListAsync();
        }

        private async Task PopulateDropdowns(StudentViewModel model)
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

            model.Banks = await GetBanksAsync();

            model.AccountTypes = await GetAccountTypesAsync();

            model.BankBranches = model.BankId.HasValue
                ? await GetBankBranchesAsync(model.BankId.Value)
                : new List<SelectListItem>();

            model.CanEditBankingDetails = CanEditBankingDetails();
        }

        private bool CanEditBankingDetails()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("ProjectManager");
        }

        private StudentViewModel MapStudentToViewModel(Student student)
        {
            return new StudentViewModel
            {
                Id = student.Id,

                StudentNumber = student.StudentNumber,

                IdNumber = student.IdNumber,

                FirstName = student.FirstName,

                LastName = student.LastName,

                ProgrammeOrCourse = student.ProgrammeOrCourse,

                ContactNumber = student.ContactNumber,

                ExistingFaceImagePath = student.FaceImagePath,

                ClientId = student.ClientId,

                ClientName = student.Client?.Name,

                SiteId = student.SiteId,

                SiteName = student.Site?.SiteName,

                BankId = student.BankId,

                BankBranchId = student.BankBranchId,

                AccountTypeId = student.AccountTypeId,

                AccountHolderName = student.AccountHolderName,

                AccountNumber = student.AccountNumber,

                IsActive = student.IsActive
            };
        }
    }
}