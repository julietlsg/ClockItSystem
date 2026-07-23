using ClockItSystem.Models.Enums;
using ClockItSystem.Models.ViewModels;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Helpers
{
    public static class ReportFilterHelper
    {
        public static ReportFilterVisibility GetVisibility(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.StudentAttendance => new ReportFilterVisibility
                {
                    ShowStudent = true
                },

                ReportType.AttendanceRegister => new ReportFilterVisibility
                {
                    ShowClient = true,
                    ShowSite = true,
                    ShowProgramme = true
                },

                ReportType.NonAttendance => new ReportFilterVisibility
                {
                    ShowClient = true,
                    ShowSite = true,
                    ShowProgramme = true
                },

                _ => new ReportFilterVisibility()
            };
        }
    }
}