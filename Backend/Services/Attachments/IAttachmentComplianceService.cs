using ProjectManagementSystem1.Services.Attachments.Models;

namespace ProjectManagementSystem1.Services.Attachments
{
    public interface IAttachmentComplianceService
    {
        Task<AttachmentComplianceResult> ValidateAsync(int projectRequestId);
    }
}

