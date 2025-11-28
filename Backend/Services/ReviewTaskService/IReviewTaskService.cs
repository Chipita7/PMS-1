using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.ReviewTasks
{
    public interface IReviewTaskService
    {
        Task AutoCreateReviewTasksAsync(int requestId, string currentUserId);
        Task<List<ProjectRequestReviewTask>> GetTasksByRequestAsync(int requestId);
        Task<ProjectRequestReviewTask?> GetTaskByIdAsync(int taskId);

        Task<bool> CompleteTaskAsync(int taskId, string currentUserId, string? completionRemarks = null);
        Task<bool> ReassignTaskAsync(int taskId, string newAssigneeId, string newAssigneeName, string currentUserId);
        Task<bool> UpdateTaskAsync(int taskId, string taskDescription, DateTime dueDate, string currentUserId);
        Task<List<ProjectRequestReviewTask>> GetOverdueTasksAsync();
        Task<List<ProjectRequestReviewTask>> GetTasksByAssigneeAsync(string assigneeId);
        Task<List<ProjectRequestReviewTask>> CreateDynamicReviewTasksAsync(int requestId, string assigneeId, string reviewerType, string currentUserId);
        Task<bool> CheckAndTransitionWorkflowAsync(int requestId, string currentUserId);

        Task<List<ProjectRequestReviewTask>> GenerateTasksFromTemplatesAsync(int requestId, List<string> taskCodes,
    Dictionary<string, string>? assigneeOverrides, int? customDueDays, string currentUserId);

        Task<List<ProjectRequestReviewTask>> GenerateTasksForReviewerTypeAsync(int requestId, string reviewerType,
    string assigneeId, int? customDueDays, string currentUserId);


        //Task<string?> FindDefaultAssigneeAsync(int requestId, string role);
        // NEW: Manual task creation by Head of Product Management (per requirements)
        Task<ProjectRequestReviewTask> CreateReviewTaskManuallyAsync(int requestId, string assigneeId, string assigneeName, string assigneeRole, string taskDescription, DateTime dueDate, string createdByUserId);

        Task<List<ProjectRequestReviewTask>> GenerateBaselineTasksForIdeaRefinementAsync(int requestId, string currentUserId);
    }
}