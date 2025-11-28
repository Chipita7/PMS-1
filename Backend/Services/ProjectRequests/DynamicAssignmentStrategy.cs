using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public class DynamicAssignmentStrategy : IDynamicAssignmentStrategy
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DynamicAssignmentStrategy> _logger;

        public DynamicAssignmentStrategy(AppDbContext context, ILogger<DynamicAssignmentStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets active role configurations ordered by priority
        /// </summary>
        public async Task<List<AssignmentRoleConfig>> GetRolePriorityAsync()
        {
            return await _context.AssignmentRoleConfigs
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Determines primary assignee based on dynamic role configurations
        /// </summary>
        public async Task<string?> GetPrimaryAssigneeAsync(int requestId)
        {
            var assignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId && !a.CompletedDate.HasValue)
                .ToListAsync();

            if (!assignments.Any()) return null;

            // ✅ FIXED: First check for EXPLICITLY marked primary
            var explicitPrimary = assignments.FirstOrDefault(a => a.IsPrimary);
            if (explicitPrimary != null)
            {
                return explicitPrimary.AssigneeId;
            }

            // ✅ FIXED: Then use role priority for ACTIVE assignments only
            var rolePriority = await GetRolePriorityAsync();
            foreach (var roleConfig in rolePriority)
            {
                var assignment = assignments.FirstOrDefault(a => a.AssigneeRole == roleConfig.Name);
                if (assignment != null)
                {
                    return assignment.AssigneeId;
                }
            }

            return assignments.First().AssigneeId;
        }

        // ✅ FIXED: Use dynamic config to identify evaluator roles
        private async Task<List<string>> GetEvaluatorRoleNamesAsync()
        {
            return await _context.AssignmentRoleConfigs
                .Where(r => r.IsActive && 
                           (r.Code == "EVALUATOR" || 
                            r.Code == "HEAD_PRODUCT_MGMT" ||
                            r.Code == "BUSINESS_ANALYST" ||
                            r.Code == "INNOVATION_CHAPTER" ||
                            r.Code == "SUBJECT_MATTER_EXPERT" ||
                            r.Code == "HEAD_ENGINEERING" ||
                            r.Code == "INFO_SECURITY"))
                .Select(r => r.Name)
                .ToListAsync();
        }

        public async Task<string?> GetPrimaryEvaluatorAsync(int requestId)
        {
            // ✅ FIXED: Use dynamic config instead of hardcoded "Evaluator"
            var evaluatorRoleNames = await GetEvaluatorRoleNamesAsync();
            
            var primaryEvaluator = await _context.ProjectRequestAssignments
                .FirstOrDefaultAsync(a => a.ProjectRequestId == requestId &&
                                         evaluatorRoleNames.Contains(a.AssigneeRole) &&
                                         a.IsPrimaryEvaluator &&
                                         !a.CompletedDate.HasValue); // ✅ Only ACTIVE

            return primaryEvaluator?.AssigneeId;
        }

        /// <summary>
        /// Gets all active evaluators (supports multiple evaluators)
        /// </summary>
        public async Task<List<string>> GetEvaluatorsAsync(int requestId)
        {
            // ✅ FIXED: Use dynamic config instead of hardcoded "Evaluator"
            var evaluatorRoleNames = await GetEvaluatorRoleNamesAsync();
            
            return await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId &&
                           evaluatorRoleNames.Contains(a.AssigneeRole) &&
                           !a.CompletedDate.HasValue) // ✅ Only ACTIVE evaluators
                .Select(a => a.AssigneeId)
                .ToListAsync();
        }

        public async Task<bool> SetPrimaryAssigneeAsync(int requestId, string assigneeId, string currentUserId)
        {
            try
            {
                var assignment = await _context.ProjectRequestAssignments
                    .FirstOrDefaultAsync(a => a.ProjectRequestId == requestId &&
                                             a.AssigneeId == assigneeId &&
                                             !a.CompletedDate.HasValue);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment not found for request {RequestId}, assignee {AssigneeId}", requestId, assigneeId);
                    return false;
                }

                // Clear existing primary flag for this role
                var existingPrimary = await _context.ProjectRequestAssignments
                    .Where(a => a.ProjectRequestId == requestId &&
                               a.AssigneeRole == assignment.AssigneeRole &&
                               a.IsPrimary &&
                               !a.CompletedDate.HasValue)
                    .ToListAsync();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimary = false;
                }

                // Set new primary
                assignment.IsPrimary = true;

                // If this is an evaluator, also set as primary evaluator
                //if (assignment.AssigneeRole == "Evaluator")
                //{
                //    await SetPrimaryEvaluatorAsync(requestId, assigneeId, currentUserId);
                //}

                await _context.SaveChangesAsync();

                _logger.LogInformation("Set {AssigneeId} as primary {Role} for request {RequestId}",
                    assigneeId, assignment.AssigneeRole, requestId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary assignee for request {RequestId}", requestId);
                return false;
            }
        }

        public async Task<bool> SetPrimaryEvaluatorAsync(int requestId, string evaluatorId, string currentUserId)
        {
            try
            {
                // ✅ FIXED: Use dynamic config to verify evaluator assignment
                var evaluatorRoleNames = await GetEvaluatorRoleNamesAsync();
                
                // Verify this is actually an evaluator assignment
                var evaluatorAssignment = await _context.ProjectRequestAssignments
                    .FirstOrDefaultAsync(a => a.ProjectRequestId == requestId &&
                                             a.AssigneeId == evaluatorId &&
                                             evaluatorRoleNames.Contains(a.AssigneeRole) &&
                                             !a.CompletedDate.HasValue);

                if (evaluatorAssignment == null)
                {
                    _logger.LogWarning("Evaluator assignment not found for request {RequestId}, evaluator {EvaluatorId}", requestId, evaluatorId);
                    return false;
                }

                // ✅ STEP 1: Clear existing primary evaluator flags in ASSIGNMENT table
                var existingPrimaryAssignments = await _context.ProjectRequestAssignments
                    .Where(a => a.ProjectRequestId == requestId &&
                               evaluatorRoleNames.Contains(a.AssigneeRole) &&
                               a.IsPrimaryEvaluator &&
                               !a.CompletedDate.HasValue)
                    .ToListAsync();

                foreach (var existing in existingPrimaryAssignments)
                {
                    existing.IsPrimaryEvaluator = false;
                }

                // ✅ STEP 2: Clear existing primary evaluator flags in EVALUATION table
                var existingPrimaryEvaluations = await _context.ProjectRequestEvaluations
                    .Where(e => e.ProjectRequestId == requestId && e.IsPrimaryEvaluation)
                    .ToListAsync();

                foreach (var existing in existingPrimaryEvaluations)
                {
                    existing.IsPrimaryEvaluation = false;
                }

                // ✅ STEP 3: Set new primary evaluator in BOTH tables
                evaluatorAssignment.IsPrimaryEvaluator = true;

                var primaryEvaluation = await _context.ProjectRequestEvaluations
                    .FirstOrDefaultAsync(e => e.ProjectRequestId == requestId &&
                                             e.ReviewerId == evaluatorId);

                if (primaryEvaluation != null)
                {
                    primaryEvaluation.IsPrimaryEvaluation = true;
                }
                else
                {
                    _logger.LogWarning("No evaluation record found for primary evaluator {EvaluatorId} on request {RequestId}",
                        evaluatorId, requestId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Set {EvaluatorId} as primary evaluator for request {RequestId} in both assignment and evaluation tables",
                    evaluatorId, requestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting primary evaluator for request {RequestId}", requestId);
                return false;
            }
        }


        /// <summary>
        /// Validates if assignment is allowed based on role configuration
        /// </summary>
        public async Task<List<string>> ValidateAssignmentAsync(int requestId, string assigneeRole, string assigneeId)
        {
            var errors = new List<string>();

            // Normalize input role to be robust against spaces/underscores/dashes/casing
            var normalizedInput = (assigneeRole ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            var allActiveRoles = await _context.AssignmentRoleConfigs
                .Where(r => r.IsActive)
                .ToListAsync();

            var roleConfig = allActiveRoles.FirstOrDefault(r =>
                ((r.Name ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()) == normalizedInput ||
                ((r.Code ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()) == normalizedInput
            );

            if (roleConfig == null)
            {
                errors.Add($"Role '{assigneeRole}' is not configured or inactive");
                return errors;
            }

            // Use canonical role name for comparisons
            var canonical = (roleConfig.Name ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();

            if (!roleConfig.CanBeMultiple)
            {
                var existingAssignments = await _context.ProjectRequestAssignments
                    .Where(a => a.ProjectRequestId == requestId && !a.CompletedDate.HasValue)
                    .ToListAsync();

                var exists = existingAssignments.Any(a =>
                    ((a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()) == canonical);

                if (exists)
                {
                    errors.Add($"Only one active '{roleConfig.Name}' assignment is allowed per request");
                }
            }

            var userAssignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId && a.AssigneeId == assigneeId && !a.CompletedDate.HasValue)
                .ToListAsync();

            var userExistingAssignment = userAssignments.Any(a =>
                ((a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()) == canonical);

            if (userExistingAssignment)
            {
                errors.Add($"User {assigneeId} is already assigned as {roleConfig.Name} for this request");
            }

            return errors;
        }

        public async Task<List<string>> GetUserRolesAsync(int requestId, string userId)
        {
            var assignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId &&
                           a.AssigneeId == userId &&
                           !a.CompletedDate.HasValue)
                .ToListAsync();

            return assignments.Select(a => a.AssigneeRole).ToList();
        }


    }
}