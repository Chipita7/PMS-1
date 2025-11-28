using ProjectManagementSystem1.Models.Enums;

namespace ProjectManagementSystem1.Model.Dto.RequestAttachmentDto
{
    public class AttachmentDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public FileType FileType { get; set; }
        public AttachmentCategory FileCategory { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public bool CanDelete { get; set; }
    }

   
}
