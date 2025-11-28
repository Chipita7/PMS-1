using Hangfire.Storage.Monitoring;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Responses;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;

namespace ProjectManagementSystem1.Services.WorkflowService
{
    public interface IWorkflowService
    {
        // Status Management
        Task<bool> CanTransitionToStatusAsync(int requestId, int newStatusConfigId, string userId);
        Task<WorkflowTransitionResult> TransitionToStatusAsync(int requestId, int newStatusConfigId, string userId, string remarks);

        // Stage Management  
        Task<bool> UpdateWorkflowStageAsync(int requestId, int workflowStageConfigId, string userId);

        // Validation
        Task<List<string>> ValidateTransitionAsync(int requestId, int newStatusConfigId, string userId);

        // History
        Task<List<StatusHistoryDto>> GetStatusHistoryAsync(int requestId);
        Task<List<WorkflowHistoryDto>> GetWorkflowHistoryAsync(int requestId);
    }

    public class WorkflowTransitionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
    }
}