using ProjectManagementSystem1.Model.Dto.AuditDto;

public interface IProjectRequestAuditService
{
    Task LogCreationAsync(int projectRequestId, string createdBy, object initialData, string? ipAddress = null);
    Task LogUpdateAsync(int projectRequestId, string updatedBy, Dictionary<string, ChangeDetail> changes, string? ipAddress = null);
    Task LogStatusChangeAsync(int projectRequestId, string changedBy, string oldStatus, string newStatus, string? ipAddress = null);
    Task LogOwnerChangeAsync(int projectRequestId, string changedBy, string oldOwner, string newOwner, string? ipAddress = null);
    Task LogWorkflowActionAsync(int projectRequestId, string actionBy, string workflowAction, string details, string? ipAddress = null);
    Task LogEntityActionAsync(int projectRequestId, string actionBy, string entityType, int entityId, string action, string description, string? ipAddress = null);
    Task<List<ProjectRequestAuditDto>> GetAuditTrailAsync(int projectRequestId);
    Task<AuditSummaryDto> GetAuditSummaryAsync(int projectRequestId);
    Task<List<ProjectRequestAuditDto>> GetRecentActivityAsync(int projectRequestId, int count = 50);
}