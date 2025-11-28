using ProjectManagementSystem1.Models.Enums;
using RequestAttachmentCategory = ProjectManagementSystem1.Models.Enums.AttachmentCategory;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.RequestAttachmentDto
{
    public class UploadProjectRequestAttachmentDto
    {
        [Required]
        public int ProjectRequestId { get; set; }

        [Required]
        public IFormFile File { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public RequestAttachmentCategory FileCategory { get; set; }
    }

    public class ProjectRequestAttachmentDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public FileType FileType { get; set; }
        public RequestAttachmentCategory FileCategory { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedOn { get; set; }
        public string? Links { get; set; }
        public long FileSize { get; set; }
    }

    public class UpdateProjectRequestAttachmentDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        public RequestAttachmentCategory? FileCategory { get; set; }
    }
    public class ProjectRequestAttachmentStatsDto
    {
        public int TotalAttachments { get; set; }
        public long TotalSizeInBytes { get; set; }
        public Dictionary<string, int> AttachmentsByCategory { get; set; } = new();
        public Dictionary<string, int> AttachmentsByType { get; set; } = new();
        public DateTime? OldestAttachment { get; set; }
        public DateTime? NewestAttachment { get; set; }
    }
    public class FileStorageResult
    {
        public string FileId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class FileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class AttachmentStatsDto
    {
        public int TotalAttachments { get; set; }
        public long TotalSizeInBytes { get; set; }
        public Dictionary<string, int> AttachmentsByCategory { get; set; } = new();
        public Dictionary<string, int> AttachmentsByType { get; set; } = new();
        public DateTime? OldestAttachment { get; set; }
        public DateTime? NewestAttachment { get; set; }
    }

    public class CategoryListDto
    {
        public List<string> Categories { get; set; } = new();
    }
}

