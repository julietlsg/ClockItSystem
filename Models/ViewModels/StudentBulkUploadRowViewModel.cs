using System;

namespace ClockItSystem.Models.ViewModels
{
    public class StudentBulkUploadRowViewModel
    {
        public int RowNumber { get; set; }

        public string StudentNumber { get; set; } = string.Empty;
        public string? IdNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProgrammeOrCourse { get; set; }
        public string? ContactNumber { get; set; }

        public string ClientName { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;

        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? AccountTypeName { get; set; }
        public string? AccountHolderName { get; set; }
        public string? AccountNumber { get; set; }

        public bool IsActive { get; set; }

        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; set; } = new();
    }
}
