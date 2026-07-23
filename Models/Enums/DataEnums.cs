using System.ComponentModel.DataAnnotations;

namespace ClockItSystem.Models.Enums
{
    public class DataEnums
    {
        public enum AttendanceRejectionReason
        {
            [Display(Name = "Student Absent")]
            StudentAbsent = 1,

            [Display(Name = "Leave")]
            Leave = 2,

            [Display(Name = "Sick Leave")]
            SickLeave = 3,

            [Display(Name = "Family Responsibility Leave")]
            FamilyResponsibilityLeave = 4
        }

        public enum ReportType
        {
            [Display(Name = "Attendance Register")]
            AttendanceRegister = 1,

            [Display(Name = "Non Attendance")]
            NonAttendance = 2,

            [Display(Name = "Student Attendance")]
            StudentAttendance = 3,

            [Display(Name = "Client Attendance Summary")]
            ClientAttendanceSummary = 4,

            [Display(Name = "Site Attendance Summary")]
            SiteAttendanceSummary = 5,

            [Display(Name = "Programme Attendance Summary")]
            ProgrammeAttendanceSummary = 6
        }

        public enum Gender
        {
            Unknown = 0,
            Male = 1,
            Female = 2
        }

        public enum Citizenship
        {
            Unknown = 0,
            SouthAfrican = 1,
            PermanentResident = 2
        }
    }
}
