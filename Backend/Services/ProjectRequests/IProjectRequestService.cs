using ProjectManagementSystem1.Model.Dto.ProjectAssignmentDto;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Requests;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Responses;
using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Dto.Workflow.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public interface IProjectRequestService
    {
        // CRUD Operations
        Task<ProjectRequestDto> CreateAsync(CreateProjectRequestDto createDto, string currentUserId);
        Task<ProjectRequestDetailDto?> GetByIdAsync(int id);
        Task<List<ProjectRequestDto>> GetAllAsync();
        Task<ProjectRequestDto?> UpdateAsync(UpdateProjectRequestDto updateDto, string currentUserId);

        // Workflow Operations
        Task<bool> UpdateStatusAsync(int requestId, int statusConfigId, string currentUserId);
        Task<bool> AssignEvaluatorAsync(int requestId, string evaluatorId, string currentUserId);
        //Task<bool> SubmitEvaluationAsync(int requestId, int feasibilityScore, int businessValueScore,
            //int technicalComplexityScore, string remarks, string currentUserId);
        Task<AssignmentResult> AssignAsync(int requestId, string team, string assigneeId, string currentUserId, string assigneeRole = "Team Member", bool setAsPrimary = false, string reviewerType = null, bool autoCreateTasks = true);
        Task<ProjectRequestDto> SubmitEvaluationAsync(int requestId, SubmitEvaluationDto evaluationDto, string currentUserId);
        Task<bool> ApproveAsync(int requestId, string remarks, string currentUserId, decimal? minimumScoreThreshold = 6.0m);
        Task<bool> CanBeApprovedAsync(int requestId, decimal? minimumScoreThreshold = 6.0m);
        Task<bool> RejectAsync(int requestId, string remarks, RejectionReason reason, string currentUserId);
        Task<bool> SetPrimaryAssigneeAsync(int requestId, string assigneeId, string currentUserId);
        Task<bool> BacklogAsync(int requestId, string reason, string currentUserId);
        Task<bool> ReactivateFromBacklogAsync(int requestId, string currentUserId);
        Task<bool> StartExecutionAsync(int requestId, string currentUserId);
        Task<bool> MarkCompletedAsync(int requestId, string currentUserId);
        Task<bool> MarkDeliveredAsync(int requestId, string currentUserId);
        Task<bool> CloseAsync(int requestId, string currentUserId);

        // Query Operations
        Task<List<ProjectRequestDto>> GetByStatusAsync(int statusConfigId);
        Task<List<ProjectRequestDto>> GetByRequesterAsync(string requesterId);
        Task<List<ProjectRequestDto>> GetAssignedToMeAsync(string userId);
        Task<List<ProjectRequestDto>> GetByWorkflowStageAsync(int workflowStageConfigId);
        Task<List<ProjectRequestDto>> GetRequestsNeedingEvaluationAsync();
        Task<List<ProjectRequestDto>> GetPendingApprovalAsync();
        Task<ApprovalDashboardDto> GetApprovalDashboardAsync();
        Task<List<ProjectRequestDto>> GetRequestsReadyForApprovalAsync();
        Task<List<StatusHistoryDto>> GetStatusHistoryAsync(int requestId);
        Task<List<WorkflowHistoryDto>> GetWorkflowHistoryAsync(int requestId);

        // Configuration
        Task<Dictionary<string, List<ConfigOptionDto>>> GetConfigurationOptionsAsync();
        Task<List<AssignmentRoleDto>> GetAssignmentRolesAsync();

        // Assignment Management
        Task<bool> CreateAssignmentAsync(int requestId, Model.Dto.WorkflowDto.Requests.CreateAssignmentDto assignmentDto, string currentUserId);
        Task<bool> CreateMultipleAssignmentsAsync(CreateMultipleAssignmentsDto assignmentsDto, string currentUserId);
        Task<List<ProjectRequestAssignment>> GetAssignmentAsync(int requestId);
        Task<PrimaryAssigneeInfo> GetPrimaryAssigneeInfoAsync(int requestId);
        Task<bool> CompleteAssignmentAsync(int assignmentId, string currentUserId);
        Task<bool> RevokeAssignmentAsync(int assignmentId, string currentUserId);

    }
}