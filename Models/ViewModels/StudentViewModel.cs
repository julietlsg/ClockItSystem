using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.ViewModels
{
    public class StudentViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required]
        [Display(Name = "Site")]
        public int SiteId { get; set; }

        public string? ClientName { get; set; }

        public string? SiteName { get; set; }

        [Required]
        [Display(Name = "Student Number")]
        [StringLength(20)]
        public string StudentNumber { get; set; } = string.Empty;

        [Display(Name = "ID Number")]
        [StringLength(20)]
        public string? IdNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Programme / Course")]
        [StringLength(150)]
        public string? ProgrammeOrCourse { get; set; }

        [Display(Name = "Contact Number")]
        [StringLength(30)]
        public string? ContactNumber { get; set; }

        [Display(Name = "Face Image")]
        public IFormFile? FaceImage { get; set; }

        public string? ExistingFaceImagePath { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        #region Banking Details

        [Display(Name = "Bank")]
        public int? BankId { get; set; }

        [Display(Name = "Branch")]
        public int? BankBranchId { get; set; }

        [Display(Name = "Account Type")]
        public int? AccountTypeId { get; set; }

        [Display(Name = "Account Holder")]
        [StringLength(150)]
        public string? AccountHolderName { get; set; }

        [Display(Name = "Account Number")]
        [StringLength(30)]
        public string? AccountNumber { get; set; }

        #endregion

        public bool CanEditBankingDetails { get; set; }
        public List<SelectListItem> Clients { get; set; }
            = new();

        public List<SelectListItem> Sites { get; set; }
            = new();

        public List<SelectListItem> Banks { get; set; }
    = new();

        public List<SelectListItem> BankBranches { get; set; }
            = new();

        public List<SelectListItem> AccountTypes { get; set; }
            = new();
    }
}