using ClockItSystem.Data;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                .OrderBy(x => x.LastName)
                .ToListAsync();

            return View(students);
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(x => x.Id == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        //[Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new StudentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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
                CreatedAt = DateTime.Now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student created successfully.";
            return RedirectToAction(nameof(Index));
        }

        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students.FindAsync(id);

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
                IsActive = student.IsActive
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

            student.StudentNumber = model.StudentNumber;
            student.IdNumber = model.IdNumber;
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.ProgrammeOrCourse = model.ProgrammeOrCourse;
            student.ContactNumber = model.ContactNumber;
            student.IsActive = model.IsActive;

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
    }
}