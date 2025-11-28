using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.RequestAttachmentDto;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Enums;
using RequestAttachmentCategory = ProjectManagementSystem1.Models.Enums.AttachmentCategory;
using ProjectManagementSystem1.Services.Audit;
using ProjectManagementSystem1.Services.FileStorageService;
using ProjectManagementSystem1.Services.RequestAttachmentService;
using System.Runtime.Intrinsics.X86;

namespace ProjectManagementSystem1.Services.Attachments
{

    public class ProjectRequestAttachmentService : IProjectRequestAttachmentService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly AppDbContext _context; // Use your actual DbContext
        private readonly ILogger<ProjectRequestAttachmentService> _logger;
        private readonly IProjectRequestAuditService _auditService;

        public ProjectRequestAttachmentService(
            IFileStorageService fileStorageService,
            AppDbContext context, // Use your actual DbContext
            ILogger<ProjectRequestAttachmentService> logger,
            IProjectRequestAuditService auditService)
        {
            _fileStorageService = fileStorageService;
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<ProjectRequestAttachmentDto> UploadAttachmentAsync(UploadProjectRequestAttachmentDto uploadDto, string uploadedBy)
        {
            // Validate request exists
            var request = await _context.ProjectRequests
                .FirstOrDefaultAsync(r => r.Id == uploadDto.ProjectRequestId);
            if (request == null)
                throw new ArgumentException($"Project request with ID {uploadDto.ProjectRequestId} not found");

            // Validate file size (10MB limit)
            if (uploadDto.File.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 10MB limit");

            // Create a unique file name
            var fileExtension = Path.GetExtension(uploadDto.File.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine("FileStorage", "ProjectRequests", uniqueFileName);

            // Save file
            await SaveFileToStorage(uploadDto.File, filePath);

            // Create attachment entity
            var attachment = new ProjectRequestAttachment
            {
                ProjectRequestId = uploadDto.ProjectRequestId,
                FileName = uploadDto.File.FileName,
                Description = uploadDto.Description,
                FileType = MapToFileType(uploadDto.File.ContentType, uploadDto.File.FileName),
                FileCategory = uploadDto.FileCategory,
                BlobId = filePath,
                UploadedBy = uploadedBy,
                UploadedOn = DateTime.UtcNow,
                Links = null
            };

            _context.ProjectRequestAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {AttachmentId} uploaded for request {RequestId} by {User}",
                attachment.Id, uploadDto.ProjectRequestId, uploadedBy);

            await _auditService.LogEntityActionAsync(uploadDto.ProjectRequestId, uploadedBy, "Attachment", attachment.Id,
                "AttachmentUploaded", $"Uploaded {attachment.FileName} ({attachment.FileCategory})");

            return MapToDto(attachment, uploadDto.File.Length);
        }

        private async Task SaveFileToStorage(IFormFile file, string filePath)
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        public async Task<List<ProjectRequestAttachmentDto>> GetAttachmentsByRequestAsync(int projectRequestId)
        {
            var attachments = await _context.ProjectRequestAttachments
                .Where(a => a.ProjectRequestId == projectRequestId)
                .OrderByDescending(a => a.UploadedOn)
                .ToListAsync();

            return attachments.Select(a => MapToDto(a)).ToList();
        }

        public async Task<ProjectRequestAttachmentDto> GetAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.ProjectRequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                throw new ArgumentException($"Attachment with ID {attachmentId} not found");

            return MapToDto(attachment);
        }

        public async Task<ProjectRequestAttachmentDto> UpdateAttachmentAsync(int attachmentId, UpdateProjectRequestAttachmentDto updateDto)
        {
            var attachment = await _context.ProjectRequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                throw new ArgumentException($"Attachment with ID {attachmentId} not found");

            // Update fields if provided
            if (updateDto.Description != null)
                attachment.Description = updateDto.Description;

            if (updateDto.FileCategory != null)
                attachment.FileCategory = updateDto.FileCategory.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {AttachmentId} updated", attachmentId);

            await _auditService.LogEntityActionAsync(attachment.ProjectRequestId, attachment.UploadedBy, "Attachment", attachment.Id,
                "AttachmentUpdated", $"Updated metadata for {attachment.FileName}");

            return MapToDto(attachment);
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.ProjectRequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                return false;

            // Delete from file storage
            await DeleteFileFromStorage(attachment.BlobId);

            // Delete from database
            _context.ProjectRequestAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Attachment {AttachmentId} deleted", attachmentId);

            await _auditService.LogEntityActionAsync(attachment.ProjectRequestId, attachment.UploadedBy, "Attachment", attachmentId,
                "AttachmentDeleted", $"Deleted {attachment.FileName}");

            return true;
        }

        private async Task DeleteFileFromStorage(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            await Task.CompletedTask;
        }

        public async Task<FileDownloadResult> DownloadAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.ProjectRequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                throw new ArgumentException($"Attachment with ID {attachmentId} not found");

            var fileStream = await GetFileFromStorage(attachment.BlobId);
            var fileInfo = new FileInfo(attachment.BlobId);

            return new FileDownloadResult
            {
                FileStream = fileStream,
                FileName = attachment.FileName,
                ContentType = GetContentType(attachment.FileType),
                FileSize = fileInfo.Exists ? fileInfo.Length : 0
            };
        }

        private async Task<Stream> GetFileFromStorage(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public async Task<object> GetAttachmentStatsAsync(int projectRequestId)
        {
            var attachments = await _context.ProjectRequestAttachments
                .Where(a => a.ProjectRequestId == projectRequestId)
                .ToListAsync();

            // Use anonymous object or your existing AttachmentStatsDto
            var stats = new
            {
                TotalAttachments = attachments.Count,
                AttachmentsByCategory = attachments
                    .GroupBy(a => a.FileCategory.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                AttachmentsByType = attachments
                    .GroupBy(a => a.FileType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                OldestAttachment = attachments.Any() ? attachments.Min(a => a.UploadedOn) : (DateTime?)null,
                NewestAttachment = attachments.Any() ? attachments.Max(a => a.UploadedOn) : (DateTime?)null
            };

            return stats;
        }

        public async Task<List<string>> GetAvailableCategoriesAsync()
        {
            return Enum.GetNames(typeof(AttachmentCategory)).ToList();
        }

        private FileType MapToFileType(string contentType, string fileName)
        {
            var lowerContentType = contentType?.ToLower() ?? "";
            var lowerFileName = fileName?.ToLower() ?? "";

            if (lowerContentType.Contains("word") || lowerContentType.Contains("document") ||
                lowerFileName.EndsWith(".doc") || lowerFileName.EndsWith(".docx"))
                return FileType.Word;

            if (lowerContentType.Contains("excel") || lowerContentType.Contains("spreadsheet") ||
                lowerFileName.EndsWith(".xls") || lowerFileName.EndsWith(".xlsx"))
                return FileType.Excel;

            if (lowerContentType.Contains("powerpoint") || lowerContentType.Contains("presentation") ||
                lowerFileName.EndsWith(".ppt") || lowerFileName.EndsWith(".pptx"))
                return FileType.PPT;

            if (lowerContentType.Contains("pdf") || lowerFileName.EndsWith(".pdf"))
                return FileType.PDF;

            if (lowerContentType.Contains("jpeg") || lowerFileName.EndsWith(".jpg") || lowerFileName.EndsWith(".jpeg"))
                return FileType.JPEG;

            if (lowerContentType.Contains("png") || lowerFileName.EndsWith(".png"))
                return FileType.PNG;

            return FileType.Other;
        }

        private string GetContentType(FileType fileType)
        {
            return fileType switch
            {
                FileType.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileType.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileType.PPT => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                FileType.PDF => "application/pdf",
                FileType.JPEG => "image/jpeg",
                FileType.JPG => "image/jpeg",
                FileType.PNG => "image/png",
                _ => "application/octet-stream"
            };
        }

        private ProjectRequestAttachmentDto MapToDto(ProjectRequestAttachment attachment, long fileSize = 0)
        {
            return new ProjectRequestAttachmentDto
            {
                Id = attachment.Id,
                ProjectRequestId = attachment.ProjectRequestId,
                FileName = attachment.FileName,
                Description = attachment.Description,
                FileType = attachment.FileType,
                FileCategory = attachment.FileCategory,
                UploadedBy = attachment.UploadedBy,
                UploadedOn = attachment.UploadedOn,
                Links = attachment.Links,
                FileSize = fileSize
            };
        }
    }
}
