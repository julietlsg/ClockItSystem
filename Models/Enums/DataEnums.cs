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
            NonAttendance = 1,
            StudentAttendance = 2,
            ClientAttendance = 3,
            ClientProgramme = 4,
            ClientStudentTotals = 5
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
