using ProjectManagementSystem1.Model.Dto.RequestAttachmentDto;

namespace ProjectManagementSystem1.Services.RequestAttachmentService
{
    public interface IProjectRequestAttachmentService
    {
        Task<ProjectRequestAttachmentDto> UploadAttachmentAsync(UploadProjectRequestAttachmentDto uploadDto, string uploadedBy);
        Task<List<ProjectRequestAttachmentDto>> GetAttachmentsByRequestAsync(int projectRequestId);
        Task<ProjectRequestAttachmentDto> GetAttachmentAsync(int attachmentId);
        Task<ProjectRequestAttachmentDto> UpdateAttachmentAsync(int attachmentId, UpdateProjectRequestAttachmentDto updateDto);
        Task<bool> DeleteAttachmentAsync(int attachmentId);
        Task<FileDownloadResult> DownloadAttachmentAsync(int attachmentId);
        Task<object> GetAttachmentStatsAsync(int projectRequestId); // Use object or your existing AttachmentStatsDto
        Task<List<string>> GetAvailableCategoriesAsync();
    }



}
