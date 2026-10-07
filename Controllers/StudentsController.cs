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
    [Authorize(Roles = "Admin,Facilitator,Project Manager")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IClientAccessService _clientAccessService;
        private readonly IWebHostEnvironment _environment;
        private readonly IPersonValidationService _personValidation;
        private readonly IStudentService _studentService;

        public StudentsController(ApplicationDbContext context,
            IWebHostEnvironment environment,
            IStudentService studentService,
            IPersonValidationService validationService,
            IClientAccessService clientAccessService)
        {
            _context = context;
            _environment = environment;
            _personValidation = validationService;
            _studentService = studentService;
            _clientAccessService = clientAccessService;
        }

        public async Task<IActionResult> Index(StudentSearchViewModel model)
        {
            var accessibleClientIds =
         await _clientAccessService.GetAccessibleClientIdsAsync();

            var query = _context.Students
                .AsNoTracking()
                .Include(s => s.Client)
                .Include(s => s.Site)
                .Where(s => accessibleClientIds.Contains(s.ClientId))
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
            // Get the clients that the currently logged-in user
            // is allowed to access.
            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            // Only retrieve the student if the student belongs
            // to one of the user's accessible clients.
            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.Client)
                .Include(s => s.Site)
                .Include(s => s.Bank)
                .Include(s => s.BankBranch)
                .Include(s => s.AccountType)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    accessibleClientIds.Contains(s.ClientId));

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

            if (!await _clientAccessService.CanAccessClientAsync(model.ClientId))
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "You do not have access to the selected client.");

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
            // Get the clients that the current user is allowed to access.
            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            // Only retrieve the student if they belong to
            // one of the user's accessible clients.
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    accessibleClientIds.Contains(s.ClientId));

            if (student == null)
                return NotFound();

            // Map the student to the existing edit ViewModel.
            var model = MapStudentToViewModel(student);

            // Populate only the dropdown data that the user
            // is allowed to work with.
            await PopulateDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            // ---------------------------------------------------------
            // 1. Get the clients the current user is allowed to access
            // ---------------------------------------------------------
            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            // ---------------------------------------------------------
            // 2. Get the existing student, but only if the student
            //    belongs to a client the current user can access.
            // ---------------------------------------------------------
            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    accessibleClientIds.Contains(s.ClientId));

            if (student == null)
                return NotFound();

            // ---------------------------------------------------------
            // 3. Validate personal information
            // ---------------------------------------------------------
            if (!_personValidation.IsValidSouthAfricanId(model.IdNumber))
            {
                ModelState.AddModelError(
                    nameof(model.IdNumber),
                    "Please enter a valid 13-digit South African ID number.");
            }

            if (!_personValidation.IsValidCellphone(model.ContactNumber))
            {
                ModelState.AddModelError(
                    nameof(model.ContactNumber),
                    "Please enter a valid South African cellphone number.");
            }

            // ---------------------------------------------------------
            // 4. Validate that the selected Client is accessible
            //    to the current user.
            // ---------------------------------------------------------
            if (!await _clientAccessService.CanAccessClientAsync(model.ClientId))
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "You do not have access to the selected client.");
            }

            // ---------------------------------------------------------
            // 5. Validate that the selected Site exists, is active,
            //    and belongs to the selected Client.
            // ---------------------------------------------------------
            var site = await _context.Sites
                .FirstOrDefaultAsync(s =>
                    s.SiteId == model.SiteId &&
                    s.ClientId == model.ClientId &&
                    s.IsActive);

            if (site == null)
            {
                ModelState.AddModelError(
                    nameof(model.SiteId),
                    "Selected site does not belong to the selected client.");
            }

            // ---------------------------------------------------------
            // 6. If validation failed, do NOT update the student.
            // ---------------------------------------------------------
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);

                return View(model);
            }

            // ---------------------------------------------------------
            // 7. Update student details
            // ---------------------------------------------------------
            student.StudentNumber = model.StudentNumber;
            student.IdNumber = model.IdNumber;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.ProgrammeOrCourse = model.ProgrammeOrCourse;
            student.ContactNumber = model.ContactNumber;
            student.IsActive = model.IsActive;

            // Only now do we change the Client/Site.
            student.ClientId = model.ClientId;
            student.SiteId = model.SiteId;

            // ---------------------------------------------------------
            // 8. Update banking details only if the current user
            //    has permission to edit them.
            // ---------------------------------------------------------
            if (CanEditBankingDetails())
            {
                student.BankId = model.BankId > 0
                    ? model.BankId
                    : null;

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

            // ---------------------------------------------------------
            // 9. Update face image if a new one was supplied
            // ---------------------------------------------------------
            if (model.FaceImage != null)
            {
                student.FaceImagePath =
                    await SaveFaceImageAsync(model.FaceImage);
            }

            // ---------------------------------------------------------
            // 10. Save changes
            // ---------------------------------------------------------
            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Student updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.InnerException?.Message ?? ex.Message);

                await PopulateDropdowns(model);

                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id)
        {
            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    accessibleClientIds.Contains(s.ClientId));
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
            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            return await _context.Clients
                .Where(c =>
                    c.IsActive &&
                    accessibleClientIds.Contains(c.ClientId))
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