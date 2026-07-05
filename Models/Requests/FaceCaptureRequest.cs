using Azure.Core;

namespace ClockItSystem.Models.Requests
{
    public class FaceCaptureRequest
    {
        public int StudentId { get; set; }
        public string ImageBase64 { get; set; } = string.Empty;
        public string DescriptorJson { get; set; } = string.Empty;

    }
}