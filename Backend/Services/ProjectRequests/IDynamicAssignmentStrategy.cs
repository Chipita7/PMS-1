using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public interface IDynamicAssignmentStrategy
    {
        Task<List<AssignmentRoleConfig>> GetRolePriorityAsync();
        Task<string?> GetPrimaryAssigneeAsync(int requestId);
        Task<string?> GetPrimaryEvaluatorAsync(int requestId);
        Task<List<string>> GetEvaluatorsAsync(int requestId);
        Task<bool> SetPrimaryAssigneeAsync(int requestId, string assigneeId, string currentUserId);
        Task<bool> SetPrimaryEvaluatorAsync(int requestId, string evaluatorId, string currentUserId);
        Task<List<string>> ValidateAssignmentAsync(int requestId, string assigneeRole, string assigneeId);
        Task<List<string>> GetUserRolesAsync(int requestId, string userId);
    }
}