using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Model.Entities.RequestConfigs;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.ReviewTasks
{
    public class ReviewTaskService : IReviewTaskService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReviewTaskService> _logger;

        public ReviewTaskService(AppDbContext context, ILogger<ReviewTaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AutoCreateReviewTasksAsync(int requestId, string currentUserId)
        {
            try
            {
                _logger.LogInformation("🔄 Auto-creating review tasks for request {RequestId}", requestId);

                var request = await _context.ProjectRequests
                    .Include(r => r.Assignments)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request {RequestId} not found for task creation", requestId);
                    return;
                }

                // Get evaluators to assign tasks to (config-driven + normalized)
                var evaluatorRoleNames = await _context.AssignmentRoleConfigs
                    .Where(r => r.IsActive && (
                        r.Code == "HEAD_PRODUCT_MGMT" ||
                        r.Code == "BUSINESS_ANALYST" ||
                        r.Code == "INNOVATION_CHAPTER" ||
                        r.Code == "SUBJECT_MATTER_EXPERT" ||
                        r.Code == "HEAD_ENGINEERING" ||
                        r.Code == "INFO_SECURITY"))
                    .Select(r => r.Name)
                    .ToListAsync();

                var evaluators = request.Assignments
                    .Where(a => !a.CompletedDate.HasValue)
                    .Where(a => evaluatorRoleNames.Any(name =>
                        (name ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant() ==
                        (a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()))
                    .ToList();

                if (!evaluators.Any())
                {
                    _logger.LogWarning("No active evaluators found for request {RequestId}", requestId);
                    return;
                }

                var tasks = new List<ProjectRequestReviewTask>();

                // Create standard review tasks for each evaluator
                foreach (var evaluator in evaluators)
                {
                    var evaluatorTasks = new[]
                    {
                        new ProjectRequestReviewTask
                        {
                            ProjectRequestId = requestId,
                            AssigneeId = evaluator.AssigneeId,
                            AssigneeName = evaluator.AssigneeId, // Would come from AD
                            AssigneeRole = evaluator.AssigneeRole,
                            TaskDescription = "Business Value Assessment - Evaluate strategic alignment and ROI",
                            AssignedById = currentUserId,
                            AssignedByName = currentUserId, // Would come from AD
                            AssignedAt = DateTime.UtcNow,
                            DueDate = DateTime.UtcNow.AddDays(3),
                            TaskStatus = "Pending"
                        },
                        new ProjectRequestReviewTask
                        {
                            ProjectRequestId = requestId,
                            AssigneeId = evaluator.AssigneeId,
                            AssigneeName = evaluator.AssigneeId,
                            AssigneeRole = evaluator.AssigneeRole,
                            TaskDescription = "Technical Feasibility Review - Assess implementation complexity",
                            AssignedById = currentUserId,
                            AssignedByName = currentUserId,
                            AssignedAt = DateTime.UtcNow,
                            DueDate = DateTime.UtcNow.AddDays(2),
                            TaskStatus = "Pending"
                        },
                        new ProjectRequestReviewTask
                        {
                            ProjectRequestId = requestId,
                            AssigneeId = evaluator.AssigneeId,
                            AssigneeName = evaluator.AssigneeId,
                            AssigneeRole = evaluator.AssigneeRole,
                            TaskDescription = "Security Impact Analysis - Review security implications",
                            AssignedById = currentUserId,
                            AssignedByName = currentUserId,
                            AssignedAt = DateTime.UtcNow,
                            DueDate = DateTime.UtcNow.AddDays(5),
                            TaskStatus = "Pending"
                        }
                    };

                    tasks.AddRange(evaluatorTasks);
                }

                await _context.ProjectRequestReviewTasks.AddRangeAsync(tasks);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Created {TaskCount} review tasks for request {RequestId}", tasks.Count, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error auto-creating review tasks for request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<List<ProjectRequestReviewTask>> CreateDynamicReviewTasksAsync(int requestId, string assigneeId, string reviewerType, string currentUserId)
        {
            // Delegate to reviewer-type generator (normalization handled there)
            return await GenerateTasksForReviewerTypeAsync(requestId, reviewerType, assigneeId, null, currentUserId);
        }

        public async Task<List<ProjectRequestReviewTask>> GenerateTasksFromTemplatesAsync(
            int requestId,
            List<string> taskCodes,
            Dictionary<string, string>? assigneeOverrides,
            int? customDueDays,
            string currentUserId)
        {
            try
            {
                _logger.LogInformation("🧩 Generating tasks from templates for request {RequestId}", requestId);

                var request = await _context.ProjectRequests.FirstOrDefaultAsync(r => r.Id == requestId);
                if (request == null) throw new ArgumentException($"Request {requestId} not found");

                var codes = (taskCodes ?? new List<string>())
                    .Select(c => (c ?? string.Empty).Trim())
                    .Where(c => c.Length > 0)
                    .ToList();
                if (!codes.Any()) return new List<ProjectRequestReviewTask>();

                var templates = await _context.ReviewTaskConfigs
                    .Where(rtc => rtc.IsActive && codes.Contains(rtc.Code))
                    .OrderBy(rtc => rtc.SortOrder)
                    .ToListAsync();

                var tasks = new List<ProjectRequestReviewTask>();

                string Norm(string s) => (s ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

                foreach (var tpl in templates)
                {
                    // Resolve assignee: override by code or role, else fallback to assignment
                    string? resolvedAssignee = null;
                    if (assigneeOverrides != null && assigneeOverrides.Count > 0)
                    {
                        var matchOverride = assigneeOverrides.FirstOrDefault(kv =>
                            Norm(kv.Key) == Norm(tpl.Code) || Norm(kv.Key) == Norm(tpl.AssigneeRole));
                        if (!matchOverride.Equals(default(KeyValuePair<string, string>)))
                            resolvedAssignee = matchOverride.Value;
                    }

                    if (string.IsNullOrWhiteSpace(resolvedAssignee))
                    {
                        resolvedAssignee = await FindDefaultAssigneeAsync(requestId, tpl.AssigneeRole);
                    }

                    if (string.IsNullOrWhiteSpace(resolvedAssignee))
                    {
                        _logger.LogWarning("No assignee resolved for template {Code}/{Role}; skipping", tpl.Code, tpl.AssigneeRole);
                        continue;
                    }

                    var dueDate = DateTime.UtcNow.AddDays(customDueDays ?? tpl.DefaultDueDays);

                    var task = new ProjectRequestReviewTask
                    {
                        ProjectRequestId = requestId,
                        AssigneeId = resolvedAssignee!,
                        AssigneeName = resolvedAssignee!,
                        AssigneeRole = tpl.AssigneeRole,
                        TaskDescription = tpl.DescriptionTemplate.Replace("{RequestTitle}", request.RequestTitle),
                        AssignedById = currentUserId,
                        AssignedByName = currentUserId,
                        AssignedAt = DateTime.UtcNow,
                        DueDate = dueDate,
                        TaskStatus = "Pending"
                    };

                    tasks.Add(task);
                }

                if (tasks.Any())
                {
                    await _context.ProjectRequestReviewTasks.AddRangeAsync(tasks);
                    await _context.SaveChangesAsync();
                }

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating tasks from templates for request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<List<ProjectRequestReviewTask>> GenerateTasksForReviewerTypeAsync(
            int requestId,
            string reviewerType,
            string assigneeId,
            int? customDueDays,
            string currentUserId)
        {
            try
            {
                _logger.LogInformation("🎯 Generating tasks for reviewer type {ReviewerType} on request {RequestId}", reviewerType, requestId);

                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null)
                    throw new ArgumentException($"Request {requestId} not found");

                // ✅ Robust: normalize reviewerType vs config.AssigneeRole for matching
                var taskConfigsAll = await _context.ReviewTaskConfigs.ToListAsync();
                var normalizedReviewerType = (reviewerType ?? string.Empty)
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToUpperInvariant();
                var taskConfigs = taskConfigsAll
                    .Where(rtc => ((rtc.AssigneeRole ?? string.Empty)
                        .Replace(" ", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("-", string.Empty)
                        .ToUpperInvariant()) == normalizedReviewerType)
                    .ToList();

                var tasks = new List<ProjectRequestReviewTask>();

                foreach (var config in taskConfigs)
                {
                    var dueDate = DateTime.UtcNow.AddDays(customDueDays ?? config.DefaultDueDays);

                    var task = new ProjectRequestReviewTask
                    {
                        ProjectRequestId = requestId,
                        AssigneeId = assigneeId,
                        AssigneeName = assigneeId,
                        AssigneeRole = config.AssigneeRole,
                        TaskDescription = config.DescriptionTemplate.Replace("{RequestTitle}", request.RequestTitle),
                        AssignedById = currentUserId,
                        AssignedByName = currentUserId,
                        AssignedAt = DateTime.UtcNow,
                        DueDate = dueDate,
                        TaskStatus = "Pending"
                    };

                    tasks.Add(task);
                }

                if (tasks.Any())
                {
                    await _context.ProjectRequestReviewTasks.AddRangeAsync(tasks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Generated {Count} tasks for reviewer type {ReviewerType}", tasks.Count, reviewerType);
                }

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating tasks for reviewer type {ReviewerType}", reviewerType);
                throw;
            }
        }

        // ✅ Helper method to find default assignee for a role
        private async Task<string?> FindDefaultAssigneeAsync(int requestId, string role)
        {
            var normalizedRole = (role ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            // First, try to find existing assignment for this role (normalized)
            var assignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId && !a.CompletedDate.HasValue)
                .ToListAsync();

            var match = assignments.FirstOrDefault(a =>
                ((a.AssigneeRole ?? string.Empty)
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToUpperInvariant()) == normalizedRole);

            if (match != null)
                return match.AssigneeId;

            // If no assignment exists, use a placeholder/default assignee
            _logger.LogInformation("No assignment found for role {Role}, using placeholder assignee", role);
            return "system-placeholder-" + (role ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        }

        public async Task<ProjectRequestReviewTask> CreateReviewTaskManuallyAsync(
            int requestId,
            string assigneeId,
            string assigneeName,
            string assigneeRole,
            string taskDescription,
            DateTime dueDate,
            string createdByUserId)
        {
            try
            {
                var request = await _context.ProjectRequests.FirstOrDefaultAsync(r => r.Id == requestId);
                if (request == null) throw new ArgumentException($"Request {requestId} not found");

                var task = new ProjectRequestReviewTask
                {
                    ProjectRequestId = requestId,
                    AssigneeId = assigneeId,
                    AssigneeName = assigneeName,
                    AssigneeRole = assigneeRole,
                    TaskDescription = taskDescription,
                    AssignedById = createdByUserId,
                    AssignedByName = createdByUserId,
                    AssignedAt = DateTime.UtcNow,
                    DueDate = dueDate,
                    TaskStatus = "Pending"
                };

                _context.ProjectRequestReviewTasks.Add(task);
                await _context.SaveChangesAsync();

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating manual review task for request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<List<ProjectRequestReviewTask>> GenerateBaselineTasksForIdeaRefinementAsync(int requestId, string currentUserId)
        {
            try
            {
                _logger.LogInformation("🧭 Generating baseline Idea Refinement tasks for request {RequestId}", requestId);

                var request = await _context.ProjectRequests
                    .Include(r => r.Assignments)
                    .FirstOrDefaultAsync(r => r.Id == requestId);
                if (request == null) throw new ArgumentException($"Request {requestId} not found");

                // Roles typically involved in Idea Refinement (config-driven by Code -> Name)
                var codes = new[] { "HEAD_ENGINEERING", "HEAD_DATA_AI", "PRODUCT_OWNER", "INFO_SECURITY", "DEVSECOPS_TEAM", "SUBJECT_MATTER_EXPERT" };
                var roleNames = await _context.AssignmentRoleConfigs
                    .Where(r => r.IsActive && codes.Contains(r.Code))
                    .Select(r => r.Name)
                    .ToListAsync();

                string Norm(string s) => (s ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant();

                var assg = request.Assignments.Where(a => !a.CompletedDate.HasValue).ToList();
                var refinementAssignees = assg.Where(a => roleNames.Any(n => Norm(n) == Norm(a.AssigneeRole))).ToList();

                var tasks = new List<ProjectRequestReviewTask>();

                foreach (var a in refinementAssignees)
                {
                    // Pull templates for this role if any
                    var activeTemplates = await _context.ReviewTaskConfigs
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.SortOrder)
                        .ToListAsync();

                    var templates = activeTemplates
                        .Where(t => Norm(t.AssigneeRole) == Norm(a.AssigneeRole))
                        .ToList();

                    if (templates.Count == 0)
                    {
                        // If no template exists, skip silently (Head PM can add manually)
                        _logger.LogInformation("No baseline templates found for role {Role}", a.AssigneeRole);
                        continue;
                    }

                    foreach (var tpl in templates)
                    {
                        var dueDate = DateTime.UtcNow.AddDays(tpl.DefaultDueDays);
                        tasks.Add(new ProjectRequestReviewTask
                        {
                            ProjectRequestId = requestId,
                            AssigneeId = a.AssigneeId,
                            AssigneeName = a.AssigneeId,
                            AssigneeRole = a.AssigneeRole,
                            TaskDescription = tpl.DescriptionTemplate.Replace("{RequestTitle}", request.RequestTitle),
                            AssignedById = currentUserId,
                            AssignedByName = currentUserId,
                            AssignedAt = DateTime.UtcNow,
                            DueDate = dueDate,
                            TaskStatus = "Pending"
                        });
                    }
                }

                if (tasks.Any())
                {
                    await _context.ProjectRequestReviewTasks.AddRangeAsync(tasks);
                    await _context.SaveChangesAsync();
                }

                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating baseline tasks for Idea Refinement for request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<List<ProjectRequestReviewTask>> GetTasksByRequestAsync(int requestId)
        {
            try
            {
                return await _context.ProjectRequestReviewTasks
                    .Where(t => t.ProjectRequestId == requestId)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting tasks for request {RequestId}", requestId);
                return new List<ProjectRequestReviewTask>();
            }
        }

        public async Task<ProjectRequestReviewTask?> GetTaskByIdAsync(int taskId)
        {
            try
            {
                return await _context.ProjectRequestReviewTasks
                    .FirstOrDefaultAsync(t => t.Id == taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting task {TaskId}", taskId);
                return null;
            }
        }

        public async Task<bool> CompleteTaskAsync(int taskId, string currentUserId, string? completionRemarks = null)
        {
            try
            {
                _logger.LogInformation("🔐 User {UserId} attempting to complete task {TaskId}", currentUserId, taskId);

                var task = await _context.ProjectRequestReviewTasks
                    .Include(t => t.ProjectRequest)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    _logger.LogWarning("❌ Task {TaskId} not found", taskId);
                    return false;
                }

                // ✅ SECURITY FIX: Verify user owns this task
                if (task.AssigneeId != currentUserId)
                {
                    _logger.LogWarning("🚫 SECURITY VIOLATION: User {UserId} attempted to complete task {TaskId} assigned to {AssigneeId}",
                        currentUserId, taskId, task.AssigneeId);
                    return false;
                }

                if (task.TaskStatus == "Completed")
                {
                    _logger.LogWarning("⚠️ Task {TaskId} is already completed", taskId);
                    return false;
                }

                // Mark task as completed
                task.TaskStatus = "Completed";
                task.CompletedAt = DateTime.UtcNow;

                // ✅ FIXED: Store completion remarks in TaskDescription if provided
                if (!string.IsNullOrWhiteSpace(completionRemarks))
                {
                    task.TaskDescription += $"\n\n[Completion Remarks - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]: {completionRemarks}";
                }

                if (!task.StartedAt.HasValue)
                {
                    task.StartedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // ✅ Check workflow transition
                try
                {
                    await CheckAndTransitionWorkflowAsync(task.ProjectRequestId, currentUserId);
                }
                catch (Exception transitionEx)
                {
                    _logger.LogError(transitionEx, "❌ Workflow transition failed, but task completion succeeded");
                }

                _logger.LogInformation("✅ Task {TaskId} completed by owner {UserId}", taskId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error completing task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> UpdateTaskAsync(int taskId, string taskDescription, DateTime dueDate, string currentUserId)
        {
            try
            {
                var task = await _context.ProjectRequestReviewTasks.FindAsync(taskId);
                if (task == null) return false;

                task.TaskDescription = taskDescription;
                task.DueDate = dueDate;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Task {TaskId} updated by {UserId}", taskId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<List<ProjectRequestReviewTask>> GetOverdueTasksAsync()
        {
            try
            {
                return await _context.ProjectRequestReviewTasks
                    .Where(t => t.DueDate < DateTime.UtcNow && t.TaskStatus != "Completed")
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting overdue tasks");
                return new List<ProjectRequestReviewTask>();
            }
        }

        public async Task<List<ProjectRequestReviewTask>> GetTasksByAssigneeAsync(string assigneeId)
        {
            try
            {
                return await _context.ProjectRequestReviewTasks
                    .Where(t => t.AssigneeId == assigneeId && t.TaskStatus != "Completed")
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting tasks for assignee {AssigneeId}", assigneeId);
                return new List<ProjectRequestReviewTask>();
            }
        }

        public async Task<bool> ReassignTaskAsync(int taskId, string newAssigneeId, string newAssigneeName, string currentUserId)
        {
            try
            {
                _logger.LogInformation("🔄 Reassigning task {TaskId} from {OldAssignee} to {NewAssignee} by {UserId}", 
                    taskId, "current", newAssigneeId, currentUserId);

                var task = await _context.ProjectRequestReviewTasks
                    .Include(t => t.ProjectRequest)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    _logger.LogWarning("❌ Task {TaskId} not found", taskId);
                    return false;
                }

                // ✅ SECURITY: Only Head Reviewer or original assigner can reassign
                // Check if current user is Head Reviewer (via owner history) or original assigner
                var isHeadReviewer = await _context.ProjectRequestOwnerHistories
                    .AnyAsync(h => h.ProjectRequestId == task.ProjectRequestId && 
                                  h.OwnerID == currentUserId && 
                                  h.Status == "Active" &&
                                  (h.OwnerRole.ToLower().Contains("head") || 
                                   h.OwnerRole.ToLower().Contains("product management")));

                var isOriginalAssigner = task.AssignedById == currentUserId;

                if (!isHeadReviewer && !isOriginalAssigner)
                {
                    _logger.LogWarning("🚫 SECURITY VIOLATION: User {UserId} attempted to reassign task {TaskId} without permission",
                        currentUserId, taskId);
                    return false;
                }

                var oldAssigneeId = task.AssigneeId;
                var oldAssigneeName = task.AssigneeName;

                // Reassign task
                task.AssigneeId = newAssigneeId;
                task.AssigneeName = newAssigneeName;
                task.AssignedById = currentUserId;
                task.AssignedByName = currentUserId;
                task.AssignedAt = DateTime.UtcNow;
                
                // Add note about reassignment
                task.TaskDescription += $"\n\n[Reassigned on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} from {oldAssigneeName} ({oldAssigneeId}) to {newAssigneeName} ({newAssigneeId}) by {currentUserId}]";

                // Reset status if it was completed
                if (task.TaskStatus == "Completed")
                {
                    task.TaskStatus = "Pending";
                    task.CompletedAt = null;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Task {TaskId} reassigned from {OldAssignee} to {NewAssignee}", 
                    taskId, oldAssigneeId, newAssigneeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reassigning task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> CheckAndTransitionWorkflowAsync(int requestId, string currentUserId)
        {
            try
            {
                _logger.LogInformation("🔄 Checking workflow transition for request {RequestId}", requestId);

                var tasks = await GetTasksByRequestAsync(requestId);

                // Check if all tasks are completed
                var allTasksCompleted = tasks.All(t => t.TaskStatus == "Completed");
                var hasTasks = tasks.Any();

                if (!hasTasks)
                {
                    _logger.LogInformation("No tasks found for request {RequestId}", requestId);
                    return false;
                }

                if (!allTasksCompleted)
                {
                    var completedCount = tasks.Count(t => t.TaskStatus == "Completed");
                    _logger.LogInformation("Request {RequestId}: {Completed}/{Total} tasks completed",
                        requestId, completedCount, tasks.Count);
                    return false;
                }

                // All tasks completed - transition workflow
                var request = await _context.ProjectRequests
                    .Include(r => r.StatusConfig)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request {RequestId} not found for workflow transition", requestId);
                    return false;
                }

                // Get "Approval Ready" workflow stage
                var approvalReadyStage = await _context.WorkflowStageConfigs
                    .FirstOrDefaultAsync(w => w.Code == "APPROVAL_READY");

                if (approvalReadyStage == null)
                {
                    _logger.LogWarning("Approval Ready workflow stage not found");
                    return false;
                }

                // Update workflow stage
                request.WorkflowStageConfigId = approvalReadyStage.Id;
                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Auto-transitioned request {RequestId} to 'Approval Ready' stage", requestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking workflow transition for request {RequestId}", requestId);
                return false;
            }
        }
    }
}