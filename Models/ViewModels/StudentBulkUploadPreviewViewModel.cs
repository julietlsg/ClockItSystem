namespace ClockItSystem.Models.ViewModels
{
    public class StudentBulkUploadPreviewViewModel
    {
        public string FileToken { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;

        public List<StudentBulkUploadRowViewModel> Rows { get; set; } = new();

        public int TotalRows => Rows.Count;
        public int ValidRows => Rows.Count(r => r.IsValid);
        public int InvalidRows => Rows.Count(r => !r.IsValid);

        public bool CanImport => TotalRows > 0 && InvalidRows == 0;
    }
}
