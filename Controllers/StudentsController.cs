using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Xml;
using System.Xml.Xsl;

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

        #region StudentBulkUpload

        private const string BulkUploadFolderName = "bulkUpload";
        private const string BulkUploadContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static readonly string[] BulkUploadRequiredHeaders =
        {
            "Student Number",
            "ID Number",
            "First Name",
            "Last Name",
            "Programme / Course",
            "Contact Number",
            "Client",
            "Site",
            "Bank",
            "Branch",
            "Account Type",
            "Account Holder Name",
            "Account Number",
            "Active"
        };

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult BulkUpload()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadBulkUploadTemplate()
        {
            var templatePath = Path.Combine(
                _environment.WebRootPath,
                "templates",
                "ClockIT_Student_Bulk_Upload_Template.xlsx");

            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound(
                    "The ClockIT Student Bulk Upload template could not be found.");
            }

            try
            {
                var accessibleClientIds =
                    await _clientAccessService.GetAccessibleClientIdsAsync();

                var clients = await _context.Clients
                    .AsNoTracking()
                    .Where(c =>
                        c.IsActive &&
                        accessibleClientIds.Contains(c.ClientId))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var clientIds = clients
                    .Select(c => c.ClientId)
                    .ToList();

                var sites = await _context.Sites
                    .AsNoTracking()
                    .Where(s =>
                        s.IsActive &&
                        clientIds.Contains(s.ClientId))
                    .OrderBy(s => s.ClientId)
                    .ThenBy(s => s.SiteName)
                    .ThenBy(s => s.SiteCode)
                    .ToListAsync();

                using var workbook = new XLWorkbook(templatePath);

                var uploadSheet = workbook.Worksheets
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            "Student Upload",
                            StringComparison.OrdinalIgnoreCase));

                if (uploadSheet == null)
                {
                    return NotFound(
                        "The Student Upload worksheet could not be found in the template.");
                }

                var referenceSheet = workbook.Worksheets
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            "Reference Values",
                            StringComparison.OrdinalIgnoreCase));

                if (referenceSheet == null)
                {
                    referenceSheet = workbook.AddWorksheet("Reference Values");
                }

                // Remove the two example records from the template.
                // The uploaded workbook will start accepting real data from row 2.
                for (var row = 2; row <= 3; row++)
                {
                    for (var column = 1; column <= BulkUploadRequiredHeaders.Length; column++)
                    {
                        uploadSheet.Cell(row, column).Value = string.Empty;
                    }
                }

                // Clear the generated reference data.
                referenceSheet.Clear();

                // The official template may already contain named ranges from a
                // previous generated version. ClosedXML does not overwrite an
                // existing defined name when Add() is called; it throws a
                // duplicate-key exception instead. These names are generated
                // implementation details for the Client/Site dropdowns, so
                // remove them before rebuilding them from the current database.
                workbook.NamedRanges.DeleteAll();

                referenceSheet.Cell(1, 1).Value = "Client Name";
                referenceSheet.Cell(1, 2).Value = "Named Range";
                referenceSheet.Cell(1, 4).Value = "Site Lists";

                var clientStartRow = 2;

                for (var i = 0; i < clients.Count; i++)
                {
                    var client = clients[i];
                    var clientRow = clientStartRow + i;
                    var namedRangeName = $"ClockITClientSites_{i + 1}";

                    referenceSheet.Cell(clientRow, 1).Value =
                        client.Name.Trim();

                    referenceSheet.Cell(clientRow, 2).Value =
                        namedRangeName;

                    var clientSites = sites
                        .Where(s => s.ClientId == client.ClientId)
                        .OrderBy(s => s.SiteName)
                        .ThenBy(s => s.SiteCode)
                        .ToList();

                    // One site list per client, starting in column D.
                    var siteColumn = 4 + i;
                    var siteStartRow = 2;

                    if (clientSites.Count == 0)
                    {
                        referenceSheet.Cell(
                            siteStartRow,
                            siteColumn).Value = string.Empty;
                    }
                    else
                    {
                        for (var siteIndex = 0;
                             siteIndex < clientSites.Count;
                             siteIndex++)
                        {
                            var site = clientSites[siteIndex];

                            referenceSheet.Cell(
                                siteStartRow + siteIndex,
                                siteColumn).Value =
                                BuildBulkUploadSiteDisplayValue(
                                    site.SiteName,
                                    site.SiteCode);
                        }
                    }

                    var siteEndRow =
                        siteStartRow +
                        Math.Max(1, clientSites.Count) -
                        1;

                    var siteRange = referenceSheet.Range(
                        referenceSheet.Cell(siteStartRow, siteColumn),
                        referenceSheet.Cell(siteEndRow, siteColumn));

                    workbook.NamedRanges.Add(
                        namedRangeName,
                        siteRange);
                }

                if (clients.Count > 0)
                {
                    var clientEndRow =
                        clientStartRow + clients.Count - 1;

                    var clientRange = referenceSheet.Range(
                        referenceSheet.Cell(clientStartRow, 1),
                        referenceSheet.Cell(clientEndRow, 1));

                    workbook.NamedRanges.Add(
                        "ClockITClients",
                        clientRange);
                }
                else
                {
                    referenceSheet.Cell(2, 1).Value = string.Empty;

                    workbook.NamedRanges.Add(
                        "ClockITClients",
                        referenceSheet.Range("A2:A2"));
                }

                // Client dropdown - column G.
                var clientValidation = uploadSheet
                    .Range("G2:G5000")
                    .CreateDataValidation();

                clientValidation.IgnoreBlanks = true;
                clientValidation.InCellDropdown = true;
                clientValidation.List("=ClockITClients");

                // Site dropdown - column H.
                //
                // The list is dependent on the Client selected in column G.
                // The selected Site is displayed as:
                //
                //     Site Name (Site Code)
                //
                var siteValidation = uploadSheet
                    .Range("H2:H5000")
                    .CreateDataValidation();

                siteValidation.IgnoreBlanks = true;
                siteValidation.InCellDropdown = true;
                siteValidation.List(
                    "=INDIRECT(\"ClockITClientSites_\"&MATCH($G2,ClockITClients,0))");

                uploadSheet.Column(7).Width = 30;
                uploadSheet.Column(8).Width = 40;

                // Keep the header visible while scrolling.
                uploadSheet.SheetView.FreezeRows(1);

                // Hide implementation/reference data.
                referenceSheet.Visibility =
                    XLWorksheetVisibility.VeryHidden;

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                return File(
                    stream.ToArray(),
                    BulkUploadContentType,
                    "ClockIT_Student_Bulk_Upload_Template.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    $"The ClockIT Excel template could not be generated: {ex.Message}");
            }
        }

        private static string BuildBulkUploadSiteDisplayValue(
            string siteName,
            string siteCode)
        {
            return $"{siteName.Trim()} ({siteCode.Trim()})";
        }

        private static bool TrySplitBulkUploadSiteValue(
            string value,
            out string siteName,
            out string siteCode)
        {
            siteName = string.Empty;
            siteCode = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();

            var openParen =
                trimmed.LastIndexOf(" (", StringComparison.Ordinal);

            if (openParen <= 0 ||
                !trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                return false;
            }

            siteName = trimmed[..openParen].Trim();
            siteCode = trimmed[(openParen + 2)..^1].Trim();

            return !string.IsNullOrWhiteSpace(siteName) &&
                   !string.IsNullOrWhiteSpace(siteCode);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(
                    "file",
                    "Please select an Excel file to upload.");

                return View();
            }

            if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "file",
                    "Only Excel .xlsx files are supported.");

                return View();
            }

            const long maxFileSize = 10 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "file",
                    "The uploaded file is too large. The maximum allowed size is 10 MB.");

                return View();
            }

            var token = Guid.NewGuid().ToString("N");
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                BulkUploadFolderName);

            var storedFilePath = Path.Combine(
                uploadFolder,
                $"{token}.xlsx");

            try
            {
                Directory.CreateDirectory(uploadFolder);

                await using (var output = new FileStream(
                    storedFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(output);
                }

                var preview = await ValidateBulkUploadFileAsync(
                    storedFilePath,
                    token,
                    file.FileName);

                if (preview == null)
                {
                    DeleteBulkUploadFile(token);

                    ModelState.AddModelError(
                        "file",
                        "The uploaded workbook does not contain the required Student Upload sheet.");

                    return View();
                }

                TempData["BulkUploadFileToken"] = token;
                TempData["BulkUploadFileName"] = file.FileName;
                TempData["BulkUploadRowCount"] = preview.Rows.Count;

                return View("BulkUploadPreview", preview);
            }
            catch (XmlException)
            {
                DeleteBulkUploadFile(token);

                ModelState.AddModelError(
                    "file",
                    "ClockIT could not read this Excel workbook. Please use the official ClockIT Student Template.");

                return View();
            }
            catch (InvalidDataException ex)
            {
                DeleteBulkUploadFile(token);

                ModelState.AddModelError(
                    "file",
                    ex.Message);

                return View();
            }
            catch (Exception)
            {
                DeleteBulkUploadFile(token);

                ModelState.AddModelError(
                    "file",
                    "An error occurred while validating the uploaded file. Please check the file and try again.");

                return View();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportBulkUpload(string token)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                !Guid.TryParseExact(token, "N", out _))
            {
                TempData["Error"] =
                    "The bulk upload session is invalid. Please upload the file again.";

                return RedirectToAction(nameof(BulkUpload));
            }

            var filePath = GetBulkUploadFilePath(token);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] =
                    "The uploaded file could not be found. Please upload the file again.";

                return RedirectToAction(nameof(BulkUpload));
            }

            var originalFileName =
                TempData.Peek("BulkUploadFileName")?.ToString()
                ?? $"{token}.xlsx";

            try
            {
                // Always revalidate immediately before inserting.
                var preview = await ValidateBulkUploadFileAsync(
                    filePath,
                    token,
                    originalFileName);

                if (preview == null)
                {
                    DeleteBulkUploadFile(token);

                    TempData["Error"] =
                        "The Student Upload worksheet could not be found. Please upload the official template again.";

                    return RedirectToAction(nameof(BulkUpload));
                }

                if (!preview.CanImport)
                {
                    TempData["Error"] =
                        $"Import stopped because validation found {preview.InvalidRows} invalid row(s). " +
                        "Please correct the Excel file and upload it again.";

                    return View("BulkUploadPreview", preview);
                }

                if (preview.ValidRows == 0)
                {
                    TempData["Error"] =
                        "There are no student records to import.";

                    return View("BulkUploadPreview", preview);
                }

                var studentNumbers =
                    preview.Rows
                        .Select(r => r.StudentNumber.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var idNumbers =
                    preview.Rows
                        .Where(r => !string.IsNullOrWhiteSpace(r.IdNumber))
                        .Select(r => r.IdNumber!.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Re-check duplicates immediately before insertion.
                var existingStudentNumbers =
                    await _context.Students
                        .AsNoTracking()
                        .Where(s => studentNumbers.Contains(s.StudentNumber))
                        .Select(s => s.StudentNumber)
                        .ToListAsync();

                var existingIdNumbers =
                    await _context.Students
                        .AsNoTracking()
                        .Where(s =>
                            s.IdNumber != null &&
                            idNumbers.Contains(s.IdNumber))
                        .Select(s => s.IdNumber!)
                        .ToListAsync();

                var duplicateStudentNumbers =
                    new HashSet<string>(
                        existingStudentNumbers,
                        StringComparer.OrdinalIgnoreCase);

                var duplicateIdNumbers =
                    new HashSet<string>(
                        existingIdNumbers,
                        StringComparer.OrdinalIgnoreCase);

                if (duplicateStudentNumbers.Count > 0 ||
                    duplicateIdNumbers.Count > 0)
                {
                    foreach (var row in preview.Rows)
                    {
                        if (duplicateStudentNumbers.Contains(
                                row.StudentNumber.Trim()))
                        {
                            row.Errors.Add(
                                "Student Number now exists in ClockIT. The file changed or another student was added after preview.");
                        }

                        if (!string.IsNullOrWhiteSpace(row.IdNumber) &&
                            duplicateIdNumbers.Contains(
                                row.IdNumber.Trim()))
                        {
                            row.Errors.Add(
                                "ID Number now exists in ClockIT. The file changed or another student was added after preview.");
                        }
                    }

                    return View("BulkUploadPreview", preview);
                }

                var accessibleClientIds =
                    await _clientAccessService.GetAccessibleClientIdsAsync();

                var clientsList = await _context.Clients
                    .Where(c =>
                        c.IsActive &&
                        accessibleClientIds.Contains(c.ClientId))
                    .ToListAsync();

                var clients = clientsList
                    .GroupBy(
                        c => c.Name.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                var sites = await _context.Sites
                    .Where(s =>
                        s.IsActive &&
                        accessibleClientIds.Contains(s.ClientId))
                    .ToListAsync();

                var sitesByClientNameAndCode = sites
                    .GroupBy(
                        s =>
                            $"{s.ClientId}|{s.SiteName.Trim()}|{s.SiteCode.Trim()}",
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                var banksList = await _context.Banks
                    .Where(b => b.IsActive)
                    .ToListAsync();

                var banks = banksList
                    .GroupBy(
                        b => b.BankName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                var branches = await _context.BankBranches
                    .Where(b => b.IsActive)
                    .ToListAsync();


                var accountTypesList = await _context.AccountTypes
                    .Where(a => a.IsActive)
                    .ToListAsync();

                var accountTypes = accountTypesList
                    .GroupBy(
                        a => a.AccountTypeName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    foreach (var row in preview.Rows)
                    {
                        var client =
                            clients[row.ClientName.Trim()];

                        if (!TrySplitBulkUploadSiteValue(
                                row.SiteName,
                                out var siteName,
                                out var siteCode))
                        {
                            throw new InvalidDataException(
                                $"Row {row.RowNumber} contains an invalid Site value.");
                        }

                        var siteKey =
                            $"{client.ClientId}|{siteName}|{siteCode}";

                        if (!sitesByClientNameAndCode.TryGetValue(
                                siteKey,
                                out var site))
                        {
                            throw new InvalidDataException(
                                $"Row {row.RowNumber} contains a Site that does not belong to the selected Client.");
                        }

                        int? bankId = null;
                        int? branchId = null;
                        int? accountTypeId = null;

                        var hasBanking =
                            !string.IsNullOrWhiteSpace(row.BankName) ||
                            !string.IsNullOrWhiteSpace(row.BranchName) ||
                            !string.IsNullOrWhiteSpace(row.AccountTypeName) ||
                            !string.IsNullOrWhiteSpace(row.AccountHolderName) ||
                            !string.IsNullOrWhiteSpace(row.AccountNumber);

                        if (hasBanking)
                        {
                            var bank =
                                banks[row.BankName!.Trim()];

                            bankId = bank.BankId;

                            var branch = branches.FirstOrDefault(b =>
                                b.BankId == bank.BankId &&
                                $"{b.BranchName} ({b.BranchCode})"
                                    .Equals(
                                        row.BranchName!.Trim(),
                                        StringComparison.OrdinalIgnoreCase));

                            if (branch == null)
                            {
                                throw new InvalidOperationException(
                                    $"Branch '{row.BranchName}' does not belong to bank '{bank.BankName}'.");
                            }

                            branchId = branch.BankBranchId;

                            var accountType =
                                accountTypes[row.AccountTypeName!.Trim()];

                            accountTypeId =
                                accountType.AccountTypeId;
                        }

                        var student = new Student
                        {
                            StudentNumber =
                                row.StudentNumber.Trim(),

                            IdNumber =
                                row.IdNumber?.Trim(),

                            FirstName =
                                row.FirstName.Trim(),

                            LastName =
                                row.LastName.Trim(),

                            ProgrammeOrCourse =
                                row.ProgrammeOrCourse?.Trim(),

                            ContactNumber =
                                row.ContactNumber?.Trim(),

                            IsActive =
                                row.IsActive,

                            CreatedAt =
                                DateTime.Now,

                            ClientId =
                                client.ClientId,

                            SiteId =
                                site.SiteId,

                            BankId =
                                bankId,

                            BankBranchId =
                                branchId,

                            AccountTypeId =
                                accountTypeId,

                            AccountHolderName =
                                row.AccountHolderName?.Trim(),

                            AccountNumber =
                                row.AccountNumber?.Trim(),

                            // Bulk upload intentionally does not import biometric data.
                            FaceImagePath = null
                        };

                        _context.Students.Add(student);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                var importedCount = preview.ValidRows;

                DeleteBulkUploadFile(token);

                TempData.Remove("BulkUploadFileToken");
                TempData.Remove("BulkUploadFileName");
                TempData.Remove("BulkUploadRowCount");

                var result =
                    new StudentBulkUploadImportResultViewModel
                    {
                        FileName = originalFileName,
                        ImportedCount = importedCount
                    };

                return View(
                    "BulkUploadImportSuccess",
                    result);
            }
            catch (DbUpdateException)
            {
                DeleteBulkUploadFile(token);

                TempData["Error"] =
                    "The students could not be imported because the database rejected the changes. " +
                    "No students were imported.";

                return RedirectToAction(nameof(BulkUpload));
            }
            catch (Exception)
            {
                DeleteBulkUploadFile(token);

                TempData["Error"] =
                    "An unexpected error occurred during the import. " +
                    "No students were imported.";

                return RedirectToAction(nameof(BulkUpload));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkUploadPreview(
            string? token = null)
        {
            token ??=
                TempData.Peek("BulkUploadFileToken")?.ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] =
                    "The bulk upload preview has expired. Please upload the file again.";

                return RedirectToAction(nameof(BulkUpload));
            }

            var filePath =
                GetBulkUploadFilePath(token);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] =
                    "The uploaded file could not be found. Please upload the file again.";

                return RedirectToAction(nameof(BulkUpload));
            }

            var originalFileName =
                TempData.Peek("BulkUploadFileName")?.ToString()
                ?? $"{token}.xlsx";

            try
            {
                var preview =
                    await ValidateBulkUploadFileAsync(
                        filePath,
                        token,
                        originalFileName);

                if (preview == null)
                {
                    DeleteBulkUploadFile(token);

                    TempData["Error"] =
                        "The uploaded workbook does not contain the required Student Upload sheet.";

                    return RedirectToAction(nameof(BulkUpload));
                }

                return View(preview);
            }
            catch (XsltException)
            {
                DeleteBulkUploadFile(token);

                TempData["Error"] =
                    "ClockIT could not read the uploaded Excel workbook. Please upload the official template again.";

                return RedirectToAction(nameof(BulkUpload));
            }
            catch (InvalidDataException ex)
            {
                DeleteBulkUploadFile(token);

                TempData["Error"] = ex.Message;

                return RedirectToAction(nameof(BulkUpload));
            }
        }

        private async Task<StudentBulkUploadPreviewViewModel?>
            ValidateBulkUploadFileAsync(
                string filePath,
                string token,
                string originalFileName)
        {
            using var workbook =
                new XLWorkbook(filePath);

            var worksheet =
                workbook.Worksheets.FirstOrDefault(
                    w => string.Equals(
                        w.Name.Trim(),
                        "Student Upload",
                        StringComparison.OrdinalIgnoreCase));

            if (worksheet == null)
                return null;

            for (var column = 1;
                 column <= BulkUploadRequiredHeaders.Length;
                 column++)
            {
                var actualHeader =
                    worksheet.Cell(1, column)
                        .GetString()
                        .Trim();

                if (!string.Equals(
                        actualHeader,
                        BulkUploadRequiredHeaders[column - 1],
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "The Student Upload sheet does not match the official ClockIT template.");
                }
            }

            var preview =
                new StudentBulkUploadPreviewViewModel
                {
                    FileToken = token,
                    OriginalFileName = originalFileName
                };

            var lastRow =
                worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Real upload data starts immediately below the header.
            const int dataStartRow = 2;

            if (lastRow < dataStartRow)
                return preview;

            var accessibleClientIds =
                await _clientAccessService.GetAccessibleClientIdsAsync();

            var activeClientsList =
                await _context.Clients
                    .Where(c =>
                        c.IsActive &&
                        accessibleClientIds.Contains(c.ClientId))
                    .ToListAsync();

            var activeClients =
                activeClientsList
                    .GroupBy(
                        c => c.Name.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

            var activeSites =
                await _context.Sites
                    .Where(s =>
                        s.IsActive &&
                        accessibleClientIds.Contains(s.ClientId))
                    .ToListAsync();

            var activeSitesByClientNameAndCode =
                activeSites
                    .GroupBy(
                        s =>
                            $"{s.ClientId}|{s.SiteName.Trim()}|{s.SiteCode.Trim()}",
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

            var activeBanksList =
                await _context.Banks
                    .Where(b => b.IsActive)
                    .ToListAsync();

            var activeBanks =
                activeBanksList
                    .GroupBy(
                        b => b.BankName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

            var activeBranches =
                await _context.BankBranches
                    .Where(b => b.IsActive)
                    .ToListAsync();

            var activeAccountTypesList =
                await _context.AccountTypes
                    .Where(a => a.IsActive)
                    .ToListAsync();

            var activeAccountTypes =
                activeAccountTypesList
                    .GroupBy(
                        a => a.AccountTypeName.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

            var existingStudentNumbers =
                await _context.Students
                    .AsNoTracking()
                    .Select(s => s.StudentNumber)
                    .ToListAsync();

            var existingIdNumbers =
                await _context.Students
                    .AsNoTracking()
                    .Where(s =>
                        s.IdNumber != null &&
                        s.IdNumber != "")
                    .Select(s => s.IdNumber!)
                    .ToListAsync();

            var existingStudentNumberSet =
                new HashSet<string>(
                    existingStudentNumbers,
                    StringComparer.OrdinalIgnoreCase);

            var existingIdNumberSet =
                new HashSet<string>(
                    existingIdNumbers,
                    StringComparer.OrdinalIgnoreCase);

            var studentNumbersInFile =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var idNumbersInFile =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (var rowNumber = dataStartRow;
                 rowNumber <= lastRow;
                 rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var hasData =
                    Enumerable.Range(
                            1,
                            BulkUploadRequiredHeaders.Length)
                        .Any(column =>
                            !string.IsNullOrWhiteSpace(
                                row.Cell(column).GetString()));

                if (!hasData)
                    continue;

                var item =
                    new StudentBulkUploadRowViewModel
                    {
                        RowNumber = rowNumber,

                        StudentNumber =
                            CellText(row.Cell(1)),

                        IdNumber =
                            NullIfEmpty(
                                CellText(row.Cell(2))),

                        FirstName =
                            CellText(row.Cell(3)),

                        LastName =
                            CellText(row.Cell(4)),

                        ProgrammeOrCourse =
                            NullIfEmpty(
                                CellText(row.Cell(5))),

                        ContactNumber =
                            NullIfEmpty(
                                CellText(row.Cell(6))),

                        ClientName =
                            CellText(row.Cell(7)),

                        SiteName =
                            CellText(row.Cell(8)),

                        BankName =
                            NullIfEmpty(
                                CellText(row.Cell(9))),

                        BranchName =
                            NullIfEmpty(
                                CellText(row.Cell(10))),

                        AccountTypeName =
                            NullIfEmpty(
                                CellText(row.Cell(11))),

                        AccountHolderName =
                            NullIfEmpty(
                                CellText(row.Cell(12))),

                        AccountNumber =
                            NullIfEmpty(
                                CellText(row.Cell(13)))
                    };

                var activeText =
                    CellText(row.Cell(14));

                // Student Number
                if (string.IsNullOrWhiteSpace(
                        item.StudentNumber))
                {
                    item.Errors.Add(
                        "Student Number is required.");
                }
                else if (item.StudentNumber.Length > 20)
                {
                    item.Errors.Add(
                        "Student Number cannot exceed 20 characters.");
                }
                else
                {
                    if (existingStudentNumberSet.Contains(
                            item.StudentNumber))
                    {
                        item.Errors.Add(
                            "Student Number already exists in ClockIT.");
                    }

                    if (!studentNumbersInFile.Add(
                            item.StudentNumber))
                    {
                        item.Errors.Add(
                            "Student Number is duplicated in this upload.");
                    }
                }

                // South African ID
                if (string.IsNullOrWhiteSpace(
                        item.IdNumber))
                {
                    item.Errors.Add(
                        "ID Number is required.");
                }
                else if (!_personValidation.IsValidSouthAfricanId(
                             item.IdNumber))
                {
                    item.Errors.Add(
                        "ID Number is not a valid 13-digit South African ID number.");
                }
                else
                {
                    if (existingIdNumberSet.Contains(
                            item.IdNumber))
                    {
                        item.Errors.Add(
                            "ID Number already exists in ClockIT.");
                    }

                    if (!idNumbersInFile.Add(
                            item.IdNumber))
                    {
                        item.Errors.Add(
                            "ID Number is duplicated in this upload.");
                    }
                }

                // First Name
                if (string.IsNullOrWhiteSpace(
                        item.FirstName))
                {
                    item.Errors.Add(
                        "First Name is required.");
                }
                else if (item.FirstName.Length > 100)
                {
                    item.Errors.Add(
                        "First Name cannot exceed 100 characters.");
                }

                // Last Name
                if (string.IsNullOrWhiteSpace(
                        item.LastName))
                {
                    item.Errors.Add(
                        "Last Name is required.");
                }
                else if (item.LastName.Length > 100)
                {
                    item.Errors.Add(
                        "Last Name cannot exceed 100 characters.");
                }

                // Programme
                if (!string.IsNullOrWhiteSpace(
                        item.ProgrammeOrCourse) &&
                    item.ProgrammeOrCourse.Length > 150)
                {
                    item.Errors.Add(
                        "Programme / Course cannot exceed 150 characters.");
                }

                // Contact Number
                if (!string.IsNullOrWhiteSpace(
                        item.ContactNumber))
                {
                    if (item.ContactNumber.Length > 30)
                    {
                        item.Errors.Add(
                            "Contact Number cannot exceed 30 characters.");
                    }

                    if (!_personValidation.IsValidCellphone(
                            item.ContactNumber))
                    {
                        item.Errors.Add(
                            "Contact Number is not a valid South African cellphone number.");
                    }
                }

                // Client + dependent Site
                if (string.IsNullOrWhiteSpace(
                        item.ClientName))
                {
                    item.Errors.Add(
                        "Client is required.");
                }
                else if (!activeClients.TryGetValue(
                             item.ClientName.Trim(),
                             out var client))
                {
                    item.Errors.Add(
                        "Client does not match an active Client in ClockIT.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(
                            item.SiteName))
                    {
                        item.Errors.Add(
                            "Site is required.");
                    }
                    else if (!TrySplitBulkUploadSiteValue(
                                 item.SiteName,
                                 out var siteName,
                                 out var siteCode))
                    {
                        item.Errors.Add(
                            "Site must be selected from the dropdown and use the format 'Site Name (Site Code)'.");
                    }
                    else
                    {
                        var siteKey =
                            $"{client.ClientId}|{siteName}|{siteCode}";

                        if (!activeSitesByClientNameAndCode.ContainsKey(
                                siteKey))
                        {
                            item.Errors.Add(
                                "Site does not match an active Site belonging to the selected Client.");
                        }
                    }
                }

                // Active
                if (activeText.Equals(
                        "TRUE",
                        StringComparison.OrdinalIgnoreCase) ||
                    activeText.Equals(
                        "YES",
                        StringComparison.OrdinalIgnoreCase) ||
                    activeText.Equals(
                        "1",
                        StringComparison.OrdinalIgnoreCase))
                {
                    item.IsActive = true;
                }
                else if (activeText.Equals(
                             "FALSE",
                             StringComparison.OrdinalIgnoreCase) ||
                         activeText.Equals(
                             "NO",
                             StringComparison.OrdinalIgnoreCase) ||
                         activeText.Equals(
                             "0",
                             StringComparison.OrdinalIgnoreCase))
                {
                    item.IsActive = false;
                }
                else
                {
                    item.Errors.Add(
                        "Active must be TRUE or FALSE.");
                }

                // Banking details are optional as a group.
                var hasAnyBankingValue =
                    !string.IsNullOrWhiteSpace(
                        item.BankName) ||
                    !string.IsNullOrWhiteSpace(
                        item.BranchName) ||
                    !string.IsNullOrWhiteSpace(
                        item.AccountTypeName) ||
                    !string.IsNullOrWhiteSpace(
                        item.AccountHolderName) ||
                    !string.IsNullOrWhiteSpace(
                        item.AccountNumber);

                if (hasAnyBankingValue)
                {
                    if (string.IsNullOrWhiteSpace(
                            item.BankName))
                    {
                        item.Errors.Add(
                            "Bank is required when banking details are supplied.");
                    }
                    else if (!activeBanks.ContainsKey(
                                 item.BankName.Trim()))
                    {
                        item.Errors.Add(
                            "Bank does not match an active Bank in ClockIT.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            item.BranchName))
                    {
                        item.Errors.Add(
                            "Branch is required when banking details are supplied.");
                    }
                    else if (!string.IsNullOrWhiteSpace(item.BankName) &&
                             activeBanks.TryGetValue(
                                 item.BankName.Trim(),
                                 out var bank))
                    {
                        var branch = activeBranches.FirstOrDefault(b =>
                            b.BankId == bank.BankId &&
                            $"{b.BranchName} ({b.BranchCode})"
                                .Equals(
                                    item.BranchName.Trim(),
                                    StringComparison.OrdinalIgnoreCase));

                        if (branch == null)
                        {
                            item.Errors.Add(
                                "Branch does not match the selected Bank.");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(
                            item.AccountTypeName))
                    {
                        item.Errors.Add(
                            "Account Type is required when banking details are supplied.");
                    }
                    else if (!activeAccountTypes.ContainsKey(
                                 item.AccountTypeName.Trim()))
                    {
                        item.Errors.Add(
                            "Account Type does not match an active Account Type in ClockIT.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            item.AccountHolderName))
                    {
                        item.Errors.Add(
                            "Account Holder Name is required when banking details are supplied.");
                    }
                    else if (item.AccountHolderName.Length > 150)
                    {
                        item.Errors.Add(
                            "Account Holder Name cannot exceed 150 characters.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            item.AccountNumber))
                    {
                        item.Errors.Add(
                            "Account Number is required when banking details are supplied.");
                    }
                    else if (item.AccountNumber.Length > 30)
                    {
                        item.Errors.Add(
                            "Account Number cannot exceed 30 characters.");
                    }
                }

                preview.Rows.Add(item);
            }

            return preview;
        }

        private static string CellText(IXLCell cell)
        {
            return cell.GetFormattedString().Trim();
        }

        private static string? NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private string GetBulkUploadFilePath(string token)
        {
            return Path.Combine(
                _environment.WebRootPath,
                "uploads",
                BulkUploadFolderName,
                $"{token}.xlsx");
        }

        private void DeleteBulkUploadFile(string token)
        {
            var path = GetBulkUploadFilePath(token);

            if (!System.IO.File.Exists(path))
                return;

            try
            {
                System.IO.File.Delete(path);
            }
            catch
            {
                // Cleanup failure must not break the workflow.
            }
        }

        #endregion
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