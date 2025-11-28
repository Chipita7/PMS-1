using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.EvaluationDto;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.WorkflowService;
using ProjectManagementSystem1.Services.IdeaIntake;
using ProjectManagementSystem1.Services.ReviewTasks;

namespace ProjectManagementSystem1.Services.Evaluation
{
    public class EvaluationService : IEvaluationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EvaluationService> _logger;
        private readonly IWorkflowService _workflowService;
        private readonly IIdeaIntakeDecisionService _ideaIntakeDecisionService;
        private readonly IReviewTaskService _reviewTaskService;

        public EvaluationService(AppDbContext context, ILogger<EvaluationService> logger, IWorkflowService workflowService, IIdeaIntakeDecisionService ideaIntakeDecisionService, IReviewTaskService reviewTaskService)
        {
            _context = context;
            _logger = logger;
            _workflowService = workflowService;
            _ideaIntakeDecisionService = ideaIntakeDecisionService;
            _reviewTaskService = reviewTaskService;
        }

        public async Task<EvaluationResult> SubmitEvaluationAsync(int evaluationId, SubmitEvaluationDto evaluationDto, string currentUserId)
        {
            try
            {
                _logger.LogInformation(" Submitting evaluation {EvaluationId} by user {UserId}", evaluationId, currentUserId);

                // FIXED: Per requirements, review tasks are optional and don't block evaluation submission
                var evaluation = await _context.ProjectRequestEvaluations
                    .Include(e => e.ProjectRequestAssignment)
                    .Include(e => e.ProjectRequest)
                    .FirstOrDefaultAsync(e => e.Id == evaluationId);

                if (evaluation == null)
                {
                    _logger.LogWarning(" Evaluation {EvaluationId} not found", evaluationId);
                    return EvaluationResult.Failed("Evaluation not found");
                }

                // FIXED: Check review tasks for this evaluator
                //var incompleteTasks = evaluation.ProjectRequest.ReviewTasks
                //    .Where(t => t.AssigneeId == currentUserId && t.TaskStatus != "Completed")
                //    .ToList();

                //if (incompleteTasks.Any())
                //{
                //    var taskDescriptions = string.Join(", ", incompleteTasks.Select(t => t.TaskDescription));
                //    return EvaluationResult.Failed($"Cannot submit evaluation: {incompleteTasks.Count} review tasks pending completion: {taskDescriptions}");
                //}

                // Verify the current user owns this evaluation
                if (evaluation.ReviewerId != currentUserId)
                {
                    _logger.LogWarning(" SECURITY VIOLATION: User {UserId} attempted to submit evaluation {EvaluationId} owned by {ReviewerId}. Evaluation Status: {Status}, Project: {ProjectId}",
                        currentUserId, evaluationId, evaluation.ReviewerId, evaluation.EvaluationStatus, evaluation.ProjectRequestId);
                    return EvaluationResult.Failed("You are not authorized to submit this evaluation");
                }
                // FIXED: Per requirements, only validate the 3 core scores
                if (evaluationDto.FeasibilityScore < 1 || evaluationDto.FeasibilityScore > 10 ||
                    evaluationDto.BusinessValueScore < 1 || evaluationDto.BusinessValueScore > 10 ||
                    evaluationDto.TechnicalComplexityScore < 1 || evaluationDto.TechnicalComplexityScore > 10)
                {
                    return EvaluationResult.Failed("Feasibility, Business Value, and Technical Complexity scores must be between 1 and 10");
                }

                // Update evaluation scores
                // StrategicAlignmentScore is optional - only set if provided (per requirements)
                if (evaluationDto.StrategicAlignmentScore.HasValue && evaluationDto.StrategicAlignmentScore.Value > 0)
                {
                    evaluation.StrategicAlignmentScore = evaluationDto.StrategicAlignmentScore.Value;
                }
                evaluation.FeasibilityScore = evaluationDto.FeasibilityScore;
                evaluation.BusinessValueScore = evaluationDto.BusinessValueScore;
                evaluation.TechnicalComplexityScore = evaluationDto.TechnicalComplexityScore;
                evaluation.EvaluationRemarks = evaluationDto.EvaluationRemarks;
                evaluation.EvaluationStatus = EvaluationStatus.Submitted;
                evaluation.SubmittedAt = DateTime.UtcNow;

                // Update main request scores
                var newTotalScore = await UpdateEvaluationScoresAsync(evaluation.ProjectRequestId, currentUserId);

                // Mark assignment as completed
                if (evaluation.ProjectRequestAssignmentId.HasValue)
                {
                    var assignment = await _context.ProjectRequestAssignments
                        .FindAsync(evaluation.ProjectRequestAssignmentId.Value);

                    if (assignment == null || assignment.AssigneeId != currentUserId)
                    {
                        _logger.LogWarning(" SECURITY VIOLATION: Assignment mismatch for evaluation {EvaluationId}", evaluationId);
                        return EvaluationResult.Failed("Evaluation assignment validation failed");
                    }

                    if (assignment != null && !assignment.CompletedDate.HasValue)
                    {
                        assignment.CompletedDate = DateTime.UtcNow;
                        assignment.AssignmentNotes += $"\nEvaluation submitted on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                        _logger.LogInformation(" Marked assignment {AssignmentId} as completed", assignment.Id);
                    }
                }

                await _context.SaveChangesAsync();

                // NEW: Auto-transition to "Idea Refinement" stage when all evaluations are complete
                await CheckAndTransitionToIdeaRefinementAsync(evaluation.ProjectRequestId, currentUserId);

                _logger.LogInformation(" Evaluation {EvaluationId} submitted successfully", evaluationId);
                return EvaluationResult.Successful("Evaluation submitted successfully", newTotalScore);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, " Database error submitting evaluation {EvaluationId}", evaluationId);
                return EvaluationResult.Failed("Database error while submitting evaluation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error submitting evaluation {EvaluationId}", evaluationId);
                return EvaluationResult.Failed($"Evaluation submission failed: {ex.Message}");
            }
        }

        public async Task<List<ProjectRequestEvaluation>> GetEvaluationsByRequestAsync(int requestId)
        {
            try
            {
                return await _context.ProjectRequestEvaluations
                    .Include(e => e.ProjectRequestAssignment)
                    .Include(e => e.ProjectRequest)
                    .ThenInclude(pr => pr.StatusConfig) // Include status config
                    .Where(e => e.ProjectRequestId == requestId)
                    .OrderByDescending(e => e.IsPrimaryEvaluation)
                    .ThenBy(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error getting evaluations for request {RequestId}", requestId);
                return new List<ProjectRequestEvaluation>();
            }
        }

        public async Task<ProjectRequestEvaluation?> GetEvaluationByIdAsync(int evaluationId)
        {
            try
            {
                return await _context.ProjectRequestEvaluations
                    .Include(e => e.ProjectRequestAssignment)
                    .Include(e => e.ProjectRequest)
                    .ThenInclude(pr => pr.StatusConfig)
                    .FirstOrDefaultAsync(e => e.Id == evaluationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error getting evaluation {EvaluationId}", evaluationId);
                return null;
            }
        }

        public async Task<decimal?> UpdateEvaluationScoresAsync(int requestId, string currentUserId)
        {
            try
            {
                var evaluations = await GetEvaluationsByRequestAsync(requestId);
                // FIXED: Per requirements, only require the 3 core scores (StrategicAlignmentScore is optional)
                var submittedEvaluations = evaluations
                    .Where(e => e.EvaluationStatus == EvaluationStatus.Submitted &&
                               e.FeasibilityScore.HasValue &&
                               e.BusinessValueScore.HasValue &&
                               e.TechnicalComplexityScore.HasValue)
                    .ToList();

                if (!submittedEvaluations.Any())
                {
                    _logger.LogInformation("No submitted evaluations with scores for request {RequestId}", requestId);
                    return null;
                }

                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null)
                {
                    _logger.LogWarning("Request {RequestId} not found for score update", requestId);
                    return null;
                }

                // ✅ FIXED: Calculate average of each evaluator's individual average
                // Per user requirement: If 3 evaluators, calculate each evaluator's average (of their 3 scores),
                // then average those 3 individual averages to get TotalScore
                // Example: Eval1 avg=4.0, Eval2 avg=4.33, Eval3 avg=4.0 → TotalScore = (4.0+4.33+4.0)/3 = 4.11
                
                var individualAverages = new List<decimal>();
                decimal totalFeasibility = 0;
                decimal totalBusinessValue = 0;
                decimal totalTechnicalComplexity = 0;
                int count = submittedEvaluations.Count;

                foreach (var eval in submittedEvaluations)
                {
                    // Calculate each evaluator's individual average (of their 3 core scores)
                    decimal individualAvg = (eval.FeasibilityScore.Value + 
                                           eval.BusinessValueScore.Value + 
                                           eval.TechnicalComplexityScore.Value) / 3.0m;
                    individualAverages.Add(individualAvg);
                    
                    // Also track category totals for category averages (for display)
                    totalFeasibility += eval.FeasibilityScore.Value;
                    totalBusinessValue += eval.BusinessValueScore.Value;
                    totalTechnicalComplexity += eval.TechnicalComplexityScore.Value;
                }

                // Calculate category averages (for display purposes)
                request.FeasibilityScore = count > 0 ? (int)Math.Round(totalFeasibility / count) : null;
                request.BusinessValueScore = count > 0 ? (int)Math.Round(totalBusinessValue / count) : null;
                request.TechnicalComplexityScore = count > 0 ? (int)Math.Round(totalTechnicalComplexity / count) : null;
                
                // ✅ TotalScore = Average of all evaluators' individual averages
                if (individualAverages.Any())
                {
                    request.TotalScore = individualAverages.Average();
                }
                else
                {
                    request.TotalScore = null;
                }

                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation(" Updated main request scores for request {RequestId}. Total: {Total}", requestId, request.TotalScore);
                return request.TotalScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error updating evaluation scores for request {RequestId}", requestId);
                return null;
            }
        }

        // NEW: Auto-transition to Idea Refinement when all evaluations are complete (per requirements)
        private async Task CheckAndTransitionToIdeaRefinementAsync(int requestId, string currentUserId)
        {
            try
            {
                var request = await _context.ProjectRequests
                    .Include(r => r.Assignments)
                    .Include(r => r.WorkflowStageConfig)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) return;

                // FIXED: Use dynamic config to check workflow stage (not hardcoded)
                var initialEvalStage = await _context.WorkflowStageConfigs
                    .FirstOrDefaultAsync(w => w.Code == "INITIAL_EVAL" && w.IsActive);

                if (initialEvalStage == null || request.WorkflowStageConfigId != initialEvalStage.Id)
                {
                    return; // Not in Initial Evaluation stage or stage config not found
                }

                // FIXED: Use dynamic config to identify evaluator roles (not hardcoded)
                // ✅ FIXED: Use dynamic config to identify evaluator roles (matching assignment logic)
                var evaluatorRoleCodes = new[] { "HEAD_PRODUCT_MGMT", "BUSINESS_ANALYST", "INNOVATION_CHAPTER", "SUBJECT_MATTER_EXPERT" };
                var evaluatorRoleConfigs = await _context.AssignmentRoleConfigs
                    .Where(r => r.IsActive && evaluatorRoleCodes.Contains(r.Code))
                    .Select(r => r.Name)
                    .ToListAsync();

                // Check only ACTIVE evaluator assignments (using dynamic config)
                var evaluatorAssignments = request.Assignments
                    .Where(a => evaluatorRoleConfigs.Contains(a.AssigneeRole) && !a.CompletedDate.HasValue)
                    .ToList();

                if (!evaluatorAssignments.Any()) return;

                var submittedEvaluations = await _context.ProjectRequestEvaluations
                    .Where(e => e.ProjectRequestId == requestId &&
                               e.EvaluationStatus == EvaluationStatus.Submitted)
                    .CountAsync();

                if (submittedEvaluations >= evaluatorAssignments.Count)
                {
                    var goToIdeaRefinement = await _ideaIntakeDecisionService.ShouldGoToIdeaRefinementAsync(request);

                    if (goToIdeaRefinement)
                    {
                        var ideaRefinementStage = await _context.WorkflowStageConfigs
                            .FirstOrDefaultAsync(w => w.Code == "IDEA_REFINEMENT" && w.IsActive);

                        if (ideaRefinementStage != null)
                        {
                            await _workflowService.UpdateWorkflowStageAsync(requestId, ideaRefinementStage.Id, currentUserId);
                            _logger.LogInformation("Auto-transitioned request {RequestId} from Initial Evaluation to Idea Refinement (all {Count} evaluations complete)", requestId, submittedEvaluations);

                            try
                            {
                                await _reviewTaskService.GenerateBaselineTasksForIdeaRefinementAsync(requestId, currentUserId);
                            }
                            catch (Exception taskEx)
                            {
                                _logger.LogError(taskEx, "Error auto-generating baseline review tasks for request {RequestId}", requestId);
                            }
                        }
                    }
                    else
                    {
                        var approvalReadyStage = await _context.WorkflowStageConfigs
                            .FirstOrDefaultAsync(w => w.Code == "APPROVAL_READY" && w.IsActive);

                        if (approvalReadyStage != null)
                        {
                            await _workflowService.UpdateWorkflowStageAsync(requestId, approvalReadyStage.Id, currentUserId);
                            _logger.LogInformation("Auto-transitioned request {RequestId} from Initial Evaluation to Approval Ready (all {Count} evaluations complete, refinement not required)", requestId, submittedEvaluations);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error checking workflow transition for request {RequestId}", requestId);
                // Don't throw - workflow transition failure shouldn't break evaluation submission
            }
        }
    }
}