using System;

namespace ClockItSystem.Models.ViewModels
{
    public class StudentProfileViewModel
    {
        #region Student Information

        public int StudentId { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? IdNumber { get; set; }

        public string? ContactNumber { get; set; }

        public string? ProgrammeOrCourse { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        #endregion

        #region Organisation

        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public int SiteId { get; set; }

        public string SiteName { get; set; } = string.Empty;

        #endregion

        #region Banking

        public int? BankId { get; set; }

        public string? BankName { get; set; }

        public int? BankBranchId { get; set; }

        public string? BranchName { get; set; }

        public int? AccountTypeId { get; set; }

        public string? AccountTypeName { get; set; }

        public string? AccountHolderName { get; set; }

        /// <summary>
        /// Full account number returned by the stored procedure.
        /// Never display this directly in the UI.
        /// </summary>
        public string? AccountNumber { get; set; }

        /// <summary>
        /// Safe value for displaying in Details screens.
        /// </summary>
        public string MaskedAccountNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AccountNumber))
                    return "-";

                if (AccountNumber.Length <= 4)
                    return AccountNumber;

                return new string('*', AccountNumber.Length - 4)
                     + AccountNumber[^4..];
            }
        }

        #endregion

        #region Biometrics

        public string? FaceImagePath { get; set; }

        public bool FaceEnrolled { get; set; }

        public bool FingerprintEnrolled { get; set; }

        public DateTime? LastVerificationDate { get; set; }

        public string BiometricStatus { get; set; } = string.Empty;

        #endregion

        #region Attendance

        public int TotalAttendances { get; set; }

        public int ApprovedAttendances { get; set; }

        public DateTime? LastAttendanceDate { get; set; }

        #endregion

        #region Audit

        public DateTime? ModifiedAt { get; set; }

        public string? ModifiedBy { get; set; }

        #endregion

        #region Computed Properties

        public string Status => IsActive ? "Active" : "Inactive";

        public string AttendanceSummary =>
            $"{ApprovedAttendances} of {TotalAttendances}";

        #endregion
    }
}