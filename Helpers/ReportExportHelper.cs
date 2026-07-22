using ClockItSystem.Models.Enums;
using ClockItSystem.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static ClockItSystem.Models.Enums.DataEnums;

namespace ClockItSystem.Helpers
{
    public static class ReportExportHelper
    {
        public static List<ReportColumn> GetColumns(List<ReportResultViewModel> data)
        {
            var properties = typeof(ReportResultViewModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var columns = new List<ReportColumn>();

            foreach (var property in properties)
            {
                var values = data.Select(x => property.GetValue(x));

                // Hide columns where every value is null or empty
                if (values.All(IsNullOrEmpty))
                    continue;

                var display = property.GetCustomAttribute<DisplayAttribute>();

                columns.Add(new ReportColumn
                {
                    Header = display?.Name ?? property.Name,
                    PropertyName = property.Name,
                    Property = property
                });
            }

            return columns;
        }

        public static object? GetValue(
            ReportResultViewModel row,
            ReportColumn column)
        {
            var value = column.Property.GetValue(row);

            if (value == null)
                return string.Empty;

            switch (value)
            {
                case DateTime date:
                    return date.ToString("yyyy-MM-dd");

                case TimeSpan time:
                    return time.ToString(@"hh\:mm");

                case decimal number:
                    return number.ToString("0.00");

                case double number:
                    return number.ToString("0.00");

                case float number:
                    return number.ToString("0.00");

                default:
                    return value.ToString();
            }
        }
        public static string GetReportTitle(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.NonAttendance => "Non Attendance Report",

                ReportType.StudentAttendance => "Student Attendance Report",

                ReportType.ClientAttendance => "Client Attendance Report",

                ReportType.ClientProgramme => "Client Programme Report",

                ReportType.ClientStudentTotals => "Client Student Totals Report",

                _ => "Attendance Report"
            };
        }

        private static bool IsNullOrEmpty(object? value)
        {
            if (value == null)
                return true;

            if (value is string s)
                return string.IsNullOrWhiteSpace(s);

            return false;
        }
    }

    public class ReportColumn
    {
            public string Header { get; set; } = string.Empty;

            public string PropertyName { get; set; } = string.Empty;

            public PropertyInfo Property { get; set; } = null!;
        }
}