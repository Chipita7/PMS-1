using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Responses;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Audit;


namespace ProjectManagementSystem1.Services.WorkflowService
{
    public class WorkflowService : IWorkflowService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IProjectRequestAuditService _auditService;

        public WorkflowService(AppDbContext context, ILogger<WorkflowService> logger, IProjectRequestAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<bool> CanTransitionToStatusAsync(int requestId, int newStatusConfigId, string userId)
        {
            try
            {
                _logger.LogInformation("🔍 Checking if request {RequestId} can transition to status {NewStatusId}",
                    requestId, newStatusConfigId);

                // Get the current request with its status
                var request = await _context.ProjectRequests
                    .Include(r => r.StatusConfig)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("❌ Request {RequestId} not found", requestId);
                    return false;
                }

                // Get the current and target statuses
                var currentStatus = request.StatusConfig;
                var targetStatus = await _context.StatusConfigs.FindAsync(newStatusConfigId);

                if (targetStatus == null)
                {
                    _logger.LogWarning("❌ Target status {StatusId} not found", newStatusConfigId);
                    return false;
                }

                // BASIC RULES - These will be enhanced later with configuration
                var allowed = CanTransition(currentStatus?.Code, targetStatus.Code, request);

                _logger.LogInformation("✅ Transition from {CurrentStatus} to {TargetStatus}: {Allowed}",
                    currentStatus?.Name, targetStatus.Name, allowed);

                return allowed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking status transition for request {RequestId}", requestId);
                return false;
            }
        }

        // ACTUALLY PERFORM THE STATUS TRANSITION
        public async Task<WorkflowTransitionResult> TransitionToStatusAsync(int requestId, int newStatusConfigId, string userId, string remarks)
        {
            var result = new WorkflowTransitionResult();

            try
            {
                _logger.LogInformation("🔄 Attempting to transition request {RequestId} to status {NewStatusId}",
                    requestId, newStatusConfigId);

                // First, validate the transition is allowed
                var validationErrors = await ValidateTransitionAsync(requestId, newStatusConfigId, userId);

                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.ValidationErrors = validationErrors;
                    result.Message = "Transition validation failed";
                    _logger.LogWarning("❌ Transition validation failed: {Errors}", string.Join(", ", validationErrors));
                    return result;
                }

                // Get the request and new status
                var request = await _context.ProjectRequests
                    .Include(r => r.StatusConfig)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                var newStatus = await _context.StatusConfigs.FindAsync(newStatusConfigId);

                if (request == null || newStatus == null)
                {
                    result.Success = false;
                    result.ValidationErrors.Add("Request or status not found");
                    return result;
                }

                // Store the old status for history
                var oldStatus = request.StatusConfig;
                var oldStatusId = request.StatusConfigId;

                // PERFORM THE ACTUAL STATUS UPDATE
                request.StatusConfigId = newStatusConfigId;
                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = userId;

                if (newStatus.Code == "APPROVED")
                {
                    _logger.LogInformation("🔍 APPROVAL DEBUG - Checking evaluation scores for request {RequestId}", requestId);
                    _logger.LogInformation("🔍 FeasibilityScore: {Feasibility}, BusinessValueScore: {Business}, TechnicalScore: {Technical}",
                        request.FeasibilityScore, request.BusinessValueScore, request.TechnicalComplexityScore);

                    if (!HasEvaluationScores(request))
                    {
                        result.Success = false;
                        result.ValidationErrors.Add("Cannot approve request without evaluation scores");
                        return result;
                    }

                    request.ApprovalDate = DateTime.UtcNow;
                    _logger.LogInformation("✅ Request {RequestId} approved - setting approval date", requestId);

                    // ADD THIS: Update workflow stage to "Approved" stage
                    var approvedStage = await _context.WorkflowStageConfigs
                        .FirstOrDefaultAsync(w => w.Code == "APPROVED_STAGE" || w.Name.Contains("Approved"));

                    if (approvedStage != null)
                    {
                        request.WorkflowStageConfigId = approvedStage.Id;
                        _logger.LogInformation("✅ Updated workflow stage to 'Approved' for request {RequestId}", requestId);
                    }
                    else
                    {
                        _logger.LogInformation("No 'Approved' workflow stage found in configuration");
                    }
                }



                // CREATE STATUS HISTORY RECORD
                await CloseActiveStatusHistoriesAsync(requestId);

                var statusHistory = new ProjectRequestStatusHistory
                {
                    ProjectRequestId = requestId,
                    StatusID = newStatus.Id.ToString(),
                    StatusDescription = newStatus.Name,
                    StartDate = DateTime.UtcNow,
                    Status = "Active",
                    StatusOwner = userId
                };

                _context.ProjectRequestStatusHistories.Add(statusHistory);

                // SAVE CHANGES
                await _context.SaveChangesAsync();

                await _auditService.LogStatusChangeAsync(requestId, userId, oldStatus?.Name ?? "Unknown", newStatus.Name);

                result.Success = true;
                result.Message = $"Successfully transitioned from {oldStatus?.Name} to {newStatus.Name}";
                result.NewStatus = newStatus.Name;

                _logger.LogInformation("✅ Successfully transitioned request {RequestId} from {OldStatus} to {NewStatus}",
                    requestId, oldStatus?.Name, newStatus.Name);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transitioning request {RequestId} to status {NewStatusId}",
                    requestId, newStatusConfigId);

                result.Success = false;
                result.Message = "An error occurred during status transition";
                result.ValidationErrors.Add(ex.Message);
                return result;
            }
        }

        // UPDATE WORKFLOW STAGE (like moving between process steps)
        public async Task<bool> UpdateWorkflowStageAsync(int requestId, int workflowStageConfigId, string userId)
        {
            try
            {
                _logger.LogInformation("📋 Updating workflow stage for request {RequestId} to stage {StageId}",
                    requestId, workflowStageConfigId);

                var request = await _context.ProjectRequests.FindAsync(requestId);
                var workflowStage = await _context.WorkflowStageConfigs.FindAsync(workflowStageConfigId);

                if (request == null || workflowStage == null)
                {
                    _logger.LogWarning("❌ Request or workflow stage not found");
                    return false;
                }

                // Close previous workflow stage history if exists
                var previousStage = await _context.ProjectRequestWorkflowHistories
                    .FirstOrDefaultAsync(w => w.ProjectRequestId == requestId && w.Status == "Active");

                if (previousStage != null)
                {
                    previousStage.EndDate = DateTime.UtcNow;
                    previousStage.Status = "Inactive";
                    _logger.LogInformation("Closed previous workflow stage {StageId}", previousStage.Id);
                }

                // Create new workflow stage history
                var newWorkflowHistory = new ProjectRequestWorkflowHistory
                {
                    ProjectRequestId = requestId,
                    WorkflowDescription = workflowStage.Name,
                    StartDate = DateTime.UtcNow,
                    Status = "Active",
                    WorkflowOwner = userId
                };

                _context.ProjectRequestWorkflowHistories.Add(newWorkflowHistory);

                // Update request workflow stage
                request.WorkflowStageConfigId = workflowStageConfigId;
                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = userId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully updated workflow stage for request {RequestId} to {StageName}",
                    requestId, workflowStage.Name);

                await _auditService.LogWorkflowActionAsync(requestId, userId, "StageChanged", workflowStage.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating workflow stage for request {RequestId}", requestId);
                return false;
            }
        }

        // VALIDATE A STATUS TRANSITION (detailed validation)
        public async Task<List<string>> ValidateTransitionAsync(int requestId, int newStatusConfigId, string userId)
        {
            var errors = new List<string>();

            try
            {
                var request = await _context.ProjectRequests
                    .Include(r => r.StatusConfig)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                var newStatus = await _context.StatusConfigs.FindAsync(newStatusConfigId);

                if (request == null)
                {
                    errors.Add("Request not found");
                    return errors;
                }

                if (newStatus == null)
                {
                    errors.Add("Target status not found");
                    return errors;
                }

                var currentStatus = request.StatusConfig;

                // RULE 1: Check if transition is allowed
                if (!CanTransition(currentStatus?.Code, newStatus.Code, request))
                {
                    errors.Add($"Cannot transition from {currentStatus?.Name} to {newStatus.Name}");
                }

                // RULE 2: Check if evaluation is required before approval
                if (newStatus.Code == "APPROVED" && !HasEvaluationScores(request))
                {
                    errors.Add("Cannot approve request without evaluation scores");
                }

                // RULE 3: Check if request is already in target status
                if (currentStatus?.Id == newStatus.Id)
                {
                    errors.Add($"Request is already in {newStatus.Name} status");
                }

                // TODO: Add more rules as needed (role-based permissions, etc.)

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating transition for request {RequestId}", requestId);
                errors.Add("Error during validation");
                return errors;
            }
        }

        // GET STATUS HISTORY (audit trail)
        public async Task<List<StatusHistoryDto>> GetStatusHistoryAsync(int requestId)
        {
            try
            {
                var history = await _context.ProjectRequestStatusHistories
                    .Where(h => h.ProjectRequestId == requestId)
                    .OrderByDescending(h => h.StartDate)
                    .ToListAsync();

                var allStatuses = await _context.StatusConfigs.ToDictionaryAsync(s => s.Id.ToString(), s => s.Name);

                return history.Select(h => new StatusHistoryDto
                {
                    Id = h.Id,
                    FromStatus = allStatuses.ContainsKey(h.StatusID) ? allStatuses[h.StatusID] : h.StatusID,
                    ToStatus = h.StatusDescription,
                    ChangedBy = h.StatusOwner,
                    ChangedAt = h.StartDate,
                    Remarks = h.StatusDescription,
                    DurationInPreviousStatus = h.StatusDuration ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status history for request {RequestId}", requestId);
                return new List<StatusHistoryDto>();
            }
        }

        // GET WORKFLOW HISTORY (stage progression)
        public async Task<List<WorkflowHistoryDto>> GetWorkflowHistoryAsync(int requestId)
        {
            try
            {
                var history = await _context.ProjectRequestWorkflowHistories
                    .Where(h => h.ProjectRequestId == requestId)
                    .OrderByDescending(h => h.StartDate)
                    .ToListAsync();

                return history.Select(h => new WorkflowHistoryDto
                {
                    Id = h.Id,
                    WorkflowStage = h.WorkflowDescription,
                    Status = h.Status,
                    ChangedBy = h.WorkflowOwner,
                    StartDate = h.StartDate,
                    EndDate = h.EndDate,
                    DurationDays = h.WorkflowDuration
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow history for request {RequestId}", requestId);
                return new List<WorkflowHistoryDto>();
            }
        }

        // PRIVATE HELPER METHODS

        // CORE TRANSITION LOGIC - This is where the rules live!
        private bool CanTransition(string? currentStatusCode, string targetStatusCode, ProjectRequest request)
        {
            if (string.IsNullOrEmpty(currentStatusCode) || string.IsNullOrEmpty(targetStatusCode))
            {
                _logger.LogWarning("❌ Cannot transition - current or target status code is null/empty");
                return false;
            }
            // Simple state machine rules - will be enhanced with configuration later
            return (currentStatusCode, targetStatusCode) switch
            {
                // Submitted can go to Under Evaluation or Rejected
                ("SUBMITTED", "UNDER_EVALUATION") => true,
                ("SUBMITTED", "REJECTED") => true,

                // Under Evaluation can go to Approved, Rejected, or Backlogged
                ("UNDER_EVALUATION", "APPROVED") => HasEvaluationScores(request), // Must have scores
                ("UNDER_EVALUATION", "REJECTED") => true,
                ("UNDER_EVALUATION", "BACKLOGGED") => true,
                ("BACKLOGGED", "UNDER_EVALUATION") => true,

                // Approved can go to In Progress
                ("APPROVED", "IN_PROGRESS") => true,

                // In Progress can go to Completed or Delivered
                ("IN_PROGRESS", "COMPLETED") => true,
                ("IN_PROGRESS", "DELIVERED") => true,
                ("COMPLETED", "DELIVERED") => true,

                // Completed can go to Closed
                ("COMPLETED", "CLOSED") => true,
                ("DELIVERED", "CLOSED") => true,

                // Rejected and Closed are terminal states
                (_, "REJECTED") => currentStatusCode != "CLOSED", // Can reject unless already closed
                (_, "CLOSED") => currentStatusCode != "REJECTED", // Can close unless already rejected

                // Default: no other transitions allowed
                _ => false
            };
        }

        // CHECK IF REQUEST HAS EVALUATION SCORES
        private bool HasEvaluationScores(ProjectRequest request)
        {
            return request.FeasibilityScore.HasValue &&
                   request.FeasibilityScore.Value >= 1 &&
                   request.FeasibilityScore.Value <= 10 &&
                   request.BusinessValueScore.HasValue &&
                   request.BusinessValueScore.Value >= 1 &&
                   request.BusinessValueScore.Value <= 10 &&
                   request.TechnicalComplexityScore.HasValue &&
                   request.TechnicalComplexityScore.Value >= 1 &&
                   request.TechnicalComplexityScore.Value <= 10;
        }

        private async Task CloseActiveStatusHistoriesAsync(int requestId)
        {
            var activeHistories = await _context.ProjectRequestStatusHistories
                .Where(h => h.ProjectRequestId == requestId && h.Status == "Active")
                .ToListAsync();

            foreach (var history in activeHistories)
            {
                history.EndDate = DateTime.UtcNow;
                history.Status = "Inactive";
            }
        }


    }

}
