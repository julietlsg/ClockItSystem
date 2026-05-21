using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Programme/Course")]
        public string? ProgrammeOrCourse { get; set; }

        public string? ExistingFaceImagePath { get; set; }

        [Display(Name = "Upload Face Image")]
        public IFormFile? FaceImage { get; set; }
        
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}