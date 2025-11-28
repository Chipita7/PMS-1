using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Requests;
using ProjectManagementSystem1.Models.Dto.Workflow.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.ApprovalAnalyticsService;
using ProjectManagementSystem1.Services.Configuration;
using ProjectManagementSystem1.Services.NotificationService;
using ProjectManagementSystem1.Services.ProjectRequests;
using ProjectManagementSystem1.Services.WorkflowService;
using ProjectManagementSystem1.Services.UserService;
using ProjectManagementSystem1.Services.Attachments;
using ProjectManagementSystem1.Constants;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Linq;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectRequestsController : ControllerBase
    {
        private readonly IProjectRequestService _projectRequestService;
        private readonly ILogger<ProjectRequestsController> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly AppDbContext _context;
        private readonly IWorkflowService _workflowService;
        private readonly IDynamicAssignmentStrategy _assignmentStrategy;
        //private readonly IDataMigrationService _dataMigrationService;
        private readonly IApprovalAnalyticsService _approvalAnalyticsService;
        private readonly IProjectRequestOwnerHistoryService _ownerHistoryService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IAttachmentComplianceService _attachmentComplianceService;

        public ProjectRequestsController(IProjectRequestService projectRequestService, ILogger<ProjectRequestsController> logger
            , IConfigurationService configurationService, AppDbContext context, IWorkflowService workflowService, IDynamicAssignmentStrategy assignmentStrategy,
            IDataMigrationService dataMigrationService, IApprovalAnalyticsService approvalAnalyticsService,
            IProjectRequestOwnerHistoryService ownerHistoryService,
            INotificationService notificationService, IUserService userService,
            IAttachmentComplianceService attachmentComplianceService)
        {
            _projectRequestService = projectRequestService;
            _logger = logger;
            _configurationService = configurationService;
            _context = context;
            _workflowService = workflowService;
            _assignmentStrategy = assignmentStrategy;
            _approvalAnalyticsService = approvalAnalyticsService;
            _ownerHistoryService = ownerHistoryService;
            _notificationService = notificationService;
            _userService = userService;
            _attachmentComplianceService = attachmentComplianceService;
            //_dataMigrationService = dataMigrationService;
        }

        // Get review tasks for a request (frontend expects this under ProjectRequests controller)
        [HttpGet("{id}/review-tasks")]
        public async Task<ActionResult> GetReviewTasks(int id)
        {
            try
            {
                var tasks = await _context.ProjectRequestReviewTasks
                    .Where(t => t.ProjectRequestId == id)
                    .OrderBy(t => t.DueDate)
                    .Select(t => new
                    {
                        id = t.Id,
                        title = t.TaskDescription,
                        assignee = t.AssigneeName,
                        assigneeId = t.AssigneeId,
                        assigneeRole = t.AssigneeRole,
                        status = t.TaskStatus.ToString(),
                        dueDate = t.DueDate,
                        submittedAt = t.CompletedAt
                    })
                    .ToListAsync();

                return Ok(new { tasks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review tasks for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving review tasks" });
            }
        }

        private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";

        [HttpGet]
        public async Task<ActionResult<List<ProjectRequestDto>>> GetAll()
        {
            try
            {
                var requests = await _projectRequestService.GetAllAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all project requests");
                return StatusCode(500, new { message = "An error occurred while retrieving project requests" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectRequestDetailDto>> GetById(int id)
        {
            try
            {
                var request = await _projectRequestService.GetByIdAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = $"Project request with ID {id} not found" });
                }
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project request by ID: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the project request" });
            }
        }

        // ✅ REMOVED: Duplicate endpoint - using the detailed one below instead

        [HttpPost]
        public async Task<ActionResult<ProjectRequestDto>> Create([FromBody] CreateProjectRequestDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // In real scenario, get current user from HttpContext
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Replace with: User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var createdRequest = await _projectRequestService.CreateAsync(createDto, currentUserId);

                return CreatedAtAction(nameof(GetById), new { id = createdRequest.Id }, createdRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project request");
                return StatusCode(500, new { message = "An error occurred while creating the project request" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProjectRequestDto>> Update(int id, [FromBody] UpdateProjectRequestDto updateDto)
        {
            try
            {
                if (id != updateDto.Id)
                {
                    return BadRequest(new { message = "ID in route does not match ID in body" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Replace with actual user ID from context
                var updatedRequest = await _projectRequestService.UpdateAsync(updateDto, currentUserId);

                if (updatedRequest == null)
                {
                    return NotFound(new { message = $"Project request with ID {id} not found" });
                }

                return Ok(updatedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project request: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the project request" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _projectRequestService.UpdateStatusAsync(id, request.StatusConfigId, currentUserId);

                if (!result)
                {
                    return NotFound(new { message = $"Project request with ID {id} not found" });
                }

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for project request: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the status" });
            }
        }

        [HttpPost("{id}/backlog")]
        public async Task<ActionResult> Backlog(int id, [FromBody] BacklogRequestDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.BacklogAsync(id, dto.Reason, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to backlog request" });
            return Ok(new { message = "Request moved to backlog" });
        }

        [HttpPost("{id}/reactivate")]
        public async Task<ActionResult> Reactivate(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.ReactivateFromBacklogAsync(id, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to reactivate request" });
            return Ok(new { message = "Request reactivated" });
        }

        [HttpPost("{id}/start-execution")]
        public async Task<ActionResult> StartExecution(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.StartExecutionAsync(id, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to start execution" });
            return Ok(new { message = "Execution started" });
        }

        [HttpPost("{id}/mark-completed")]
        public async Task<ActionResult> MarkCompleted(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.MarkCompletedAsync(id, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to mark completed" });
            return Ok(new { message = "Request marked as completed" });
        }

        [HttpPost("{id}/mark-delivered")]
        public async Task<ActionResult> MarkDelivered(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.MarkDeliveredAsync(id, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to mark delivered" });
            return Ok(new { message = "Request marked as delivered" });
        }

        [HttpPost("{id}/close")]
        public async Task<ActionResult> Close(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _projectRequestService.CloseAsync(id, currentUserId);
            if (!result) return BadRequest(new { message = "Unable to close request" });
            return Ok(new { message = "Request closed" });
        }

        //[HttpPost("{id}/evaluate")]
        //public async Task<ActionResult> SubmitEvaluation(int id, [FromBody] SubmitEvaluationRequest request)
        //{
        //    try
        //    {
        //        var currentUserId = "current-user-id"; // Replace with actual user ID
        //        var result = await _projectRequestService.SubmitEvaluationAsync(
        //            id, request.FeasibilityScore, request.BusinessValueScore,
        //            request.TechnicalComplexityScore, request.Remarks, currentUserId);

        //        if (!result)
        //        {
        //            return NotFound(new { message = $"Project request with ID {id} not found" });
        //        }

        //        return Ok(new { message = "Evaluation submitted successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error submitting evaluation for project request: {Id}", id);
        //        return StatusCode(500, new { message = "An error occurred while submitting the evaluation" });
        //    }
        //}

        [HttpGet("configurations")]
        public async Task<ActionResult<Dictionary<string, List<ConfigOptionDto>>>> GetConfigurations()
        {
            try
            {
                var configs = await _projectRequestService.GetConfigurationOptionsAsync();
                return Ok(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration options");
                return StatusCode(500, new { message = "An error occurred while retrieving configuration options" });
            }
        }

        [HttpGet("assignment-roles")]
        public async Task<ActionResult> GetAssignmentRoles()
        {
            try
            {
                var roles = await _projectRequestService.GetAssignmentRolesAsync();
                return Ok(new { success = true, roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment roles");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving assignment roles" });
            }
        }

        [HttpGet("status/{statusConfigId}")]
        public async Task<ActionResult<List<ProjectRequestDto>>> GetByStatus(int statusConfigId)
        {
            try
            {
                var requests = await _projectRequestService.GetByStatusAsync(statusConfigId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests by status");
                return StatusCode(500, new { message = "An error occurred while retrieving requests by status" });
            }
        }

        [HttpPost("test-minimal")]
        public async Task<ActionResult> TestMinimalCreate()
        {
            try
            {
                var currentUserId = "test-user";

                // Create minimal DTO with only required fields
                var minimalDto = new CreateProjectRequestDto
                {
                    RequestTitle = "TEST - Minimal Request",
                    RequestDescription = "This is a test request with minimal data",
                    RequestTypeConfigId = 1,
                    RequestCategoryConfigId = 1,
                    ServiceCategoryConfigId = 1,
                    ProductCategoryConfigId = 1,
                    PriorityConfigId = 1,
                    BusinessImpactConfigId = 1,
                    RequestUrgencyConfigId = 1,
                    StrategicAlignment = "Test",
                    PrimaryContactEmail = "test.user@pms.local",
                    PrimaryContactPhone = "+251900000000",
                    RequestedByName = "Test User",
                    BusinessSector = "Digital Banking",
                    BusinessDivision = "Technology",
                    BusinessDepartment = "Digital Factory",
                    OrganizationName = "Commercial Bank of Ethiopia"
                };

                var result = await _projectRequestService.CreateAsync(minimalDto, currentUserId);
                return Ok(new
                {
                    success = true,
                    message = "Minimal test passed",
                    createdId = result.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Minimal test failed",
                    error = ex.Message,
                    detailedError = ex.ToString()
                });
            }
        }

        [HttpGet("{id}/debug-configs")]
        public async Task<ActionResult> DebugConfigs(int id)
        {
            try
            {
                var projectRequest = await _context.ProjectRequests
                    .Include(pr => pr.RequestTypeConfig)
                    .Include(pr => pr.RequestCategoryConfig)
                    .Include(pr => pr.ServiceCategoryConfig)
                    .Include(pr => pr.ProductCategoryConfig)
                    .Include(pr => pr.PriorityConfig)
                    .Include(pr => pr.BusinessImpactConfig)
                    .Include(pr => pr.RequestUrgencyConfig)
                    .Include(pr => pr.StatusConfig)
                    .Include(pr => pr.RiskLevelConfig)
                    .Include(pr => pr.ComplexityLevelConfig)
                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (projectRequest == null)
                    return NotFound();

                var debugInfo = new
                {
                    RequestId = projectRequest.Id,
                    ConfigIds = new
                    {
                        RequestTypeConfigId = projectRequest.RequestTypeConfigId,
                        RequestCategoryConfigId = projectRequest.RequestCategoryConfigId,
                        ServiceCategoryConfigId = projectRequest.ServiceCategoryConfigId,
                        ProductCategoryConfigId = projectRequest.ProductCategoryConfigId,
                        BusinessImpactConfigId = projectRequest.BusinessImpactConfigId,
                        RequestUrgencyConfigId = projectRequest.RequestUrgencyConfigId,
                        RiskLevelConfigId = projectRequest.RiskLevelConfigId,
                        ComplexityLevelConfigId = projectRequest.ComplexityLevelConfigId
                    },
                    LoadedConfigs = new
                    {
                        RequestType = projectRequest.RequestTypeConfig != null ? projectRequest.RequestTypeConfig.Name : "NULL",
                        RequestCategory = projectRequest.RequestCategoryConfig != null ? projectRequest.RequestCategoryConfig.Name : "NULL",
                        ServiceCategory = projectRequest.ServiceCategoryConfig != null ? projectRequest.ServiceCategoryConfig.Name : "NULL",
                        ProductCategory = projectRequest.ProductCategoryConfig != null ? projectRequest.ProductCategoryConfig.Name : "NULL",
                        BusinessImpact = projectRequest.BusinessImpactConfig != null ? projectRequest.BusinessImpactConfig.Name : "NULL",
                        RequestUrgency = projectRequest.RequestUrgencyConfig != null ? projectRequest.RequestUrgencyConfig.Name : "NULL",
                        RiskLevel = projectRequest.RiskLevelConfig != null ? projectRequest.RiskLevelConfig.Name : "NULL",
                        ComplexityLevel = projectRequest.ComplexityLevelConfig != null ? projectRequest.ComplexityLevelConfig.Name : "NULL"
                    }
                };

                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging configs for request {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("fix-missing-configs")]
        public async Task<ActionResult> FixMissingConfigs()
        {
            try
            {
                _logger.LogInformation("Fixing missing configurations...");

                // Request Category Configs
                if (!_context.RequestCategoryConfigs.Any())
                {
                    var requestCategories = new List<RequestCategoryConfig>
            {
                new() { Name = "Remittance", Code = "REMITTANCE", SortOrder = 1 },
                new() { Name = "Government", Code = "GOVERNMENT", SortOrder = 2 },
                new() { Name = "Inhouse-development", Code = "INHOUSE_DEV", SortOrder = 3 }
            };
                    await _context.RequestCategoryConfigs.AddRangeAsync(requestCategories);
                    _logger.LogInformation("Added RequestCategoryConfigs");
                }

                // Service Category Configs
                if (!_context.ServiceCategoryConfigs.Any())
                {
                    var serviceCategories = new List<ServiceCategoryConfig>
            {
                new() { Name = "Digital Banking Management", Code = "DIGITAL_BANKING", SortOrder = 1 },
                new() { Name = "Card Banking Management", Code = "CARD_BANKING", SortOrder = 2 },
                new() { Name = "Credit Management", Code = "CREDIT_MGMT", SortOrder = 3 },
                new() { Name = "Risk Management", Code = "RISK_MGMT", SortOrder = 4 }
            };
                    await _context.ServiceCategoryConfigs.AddRangeAsync(serviceCategories);
                    _logger.LogInformation("Added ServiceCategoryConfigs");
                }

                // Product Category Configs
                if (!_context.ProductCategoryConfigs.Any())
                {
                    var productCategories = new List<ProductCategoryConfig>
            {
                new() { Name = "Mobile Banking", Code = "MOBILE_BANKING", SortOrder = 1 },
                new() { Name = "CBE Birr Wallet", Code = "CBE_BIRR", SortOrder = 2 },
                new() { Name = "T24 Core Banking", Code = "T24_CORE", SortOrder = 3 },
                new() { Name = "IMAL Core Banking", Code = "IMAL_CORE", SortOrder = 4 },
                new() { Name = "Other Legacy System", Code = "OTHER_LEGACY", SortOrder = 5 }
            };
                    await _context.ProductCategoryConfigs.AddRangeAsync(productCategories);
                    _logger.LogInformation("Added ProductCategoryConfigs");
                }

                // Impact/Urgency Configs (used for BusinessImpact, RequestUrgency, RiskLevel, ComplexityLevel)
                if (!_context.ImpactUrgencyConfigs.Any())
                {
                    var impactUrgencies = new List<ImpactUrgencyConfig>
            {
                new() { Name = "High", Code = "HIGH", Color = "#FF0000", SortOrder = 1 },
                new() { Name = "Medium", Code = "MEDIUM", Color = "#FFA500", SortOrder = 2 },
                new() { Name = "Low", Code = "LOW", Color = "#00FF00", SortOrder = 3 }
            };
                    await _context.ImpactUrgencyConfigs.AddRangeAsync(impactUrgencies);
                    _logger.LogInformation("Added ImpactUrgencyConfigs");
                }

                await _context.SaveChangesAsync();

                // Refresh cache
                await _configurationService.RefreshCacheAsync();

                // Return the created config IDs for reference
                var configs = new
                {
                    RequestCategories = await _context.RequestCategoryConfigs.ToListAsync(),
                    ServiceCategories = await _context.ServiceCategoryConfigs.ToListAsync(),
                    ProductCategories = await _context.ProductCategoryConfigs.ToListAsync(),
                    ImpactUrgencies = await _context.ImpactUrgencyConfigs.ToListAsync()
                };

                return Ok(new
                {
                    success = true,
                    message = "Missing configurations added successfully",
                    configs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing missing configurations");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fix configurations",
                    error = ex.Message
                });
            }
        }

        // ASSIGN A REQUEST TO A TEAM/PERSON
        [HttpPut("{id}/assign")]
        public async Task<ActionResult> Assign(int id, [FromBody] Model.Dto.ProjectRequestsDto.Requests.AssignRequestDto assignDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // TODO: Get from auth

                // Use the enhanced AssignAsync that returns AssignmentResult
                var result = await _projectRequestService.AssignAsync(
                    id,
                    assignDto.AssignedTeam,
                    assignDto.AssignedTo,
                    currentUserId,
                    assignDto.AssigneeRole,
                    assignDto.SetAsPrimary,
                    assignDto.ReviewerType,
                    assignDto.AutoCreateTasks // ✅ NEW: Pass auto-create flag
                );

                // 
                if (!result.IsSuccessful)  
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Assignment failed: {string.Join(", ", result.Errors)}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Request assigned to {assignDto.AssignedTeam}/{assignDto.AssignedTo} as {assignDto.AssigneeRole}",
                    primaryAssignee = result.PrimaryAssigneeId,
                    createdAssignment = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while assigning the request" });
            }
        }

        // Assign Head Reviewer (Owner) for a request
        public class HeadAssignRequest
        {
            public string? HeadUserId { get; set; }
            public string? HeadUsername { get; set; }
            public bool? Notify { get; set; }
        }

        [HttpPut("{id}/assign-head")]
        public async Task<ActionResult> AssignHead(int id, [FromBody] HeadAssignRequest dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Ensure request exists
                var exists = await _context.ProjectRequests.AnyAsync(r => r.Id == id);
                if (!exists)
                    return NotFound(new { message = $"Project request with ID {id} not found" });

                // ✅ FIXED: Resolve target owner with better self-assignment handling
                // For self-assignment: prefer HeadUserId if provided, otherwise use currentUserId
                // For other assignment: use provided HeadUserId or HeadUsername
                var newOwnerId = !string.IsNullOrWhiteSpace(dto?.HeadUserId) ? dto.HeadUserId
                    : !string.IsNullOrWhiteSpace(dto?.HeadUsername) ? dto.HeadUsername
                    : currentUserId; // Default to current user for self-assignment
                
                // ✅ FIXED: For self-assignment, ensure we use the actual current user ID
                // Check if this is a self-assignment (no explicit HeadUserId/HeadUsername provided)
                if (string.IsNullOrWhiteSpace(dto?.HeadUserId) && string.IsNullOrWhiteSpace(dto?.HeadUsername))
                {
                    newOwnerId = currentUserId; // Force use current user ID for self-assignment
                    _logger.LogInformation("🔍 Self-assignment detected: Using currentUserId {CurrentUserId}", currentUserId);
                }
                
                var newOwnerName = !string.IsNullOrWhiteSpace(dto?.HeadUsername) ? dto.HeadUsername
                    : !string.IsNullOrWhiteSpace(dto?.HeadUserId) ? dto.HeadUserId
                    : (User.Identity?.Name ?? currentUserId);
                
                // ✅ FIXED: If name is still empty or same as ID, try to get from user service
                if (string.IsNullOrWhiteSpace(newOwnerName) || newOwnerName == newOwnerId)
                {
                    try
                    {
                        var userInfo = await _userService.GetUserByEmployeeIdAsync(newOwnerId);
                        if (userInfo != null && !string.IsNullOrWhiteSpace(userInfo.FullName))
                        {
                            newOwnerName = userInfo.FullName;
                        }
                    }
                    catch (Exception userEx)
                    {
                        _logger.LogWarning(userEx, "Could not fetch user name for {UserId}, using ID as name", newOwnerId);
                        newOwnerName = newOwnerId;
                    }
                }

                // ✅ IMPORTANT: Head Reviewer Role Explanation
                // - "Head Reviewer" is the workflow owner for a request (assigned via this endpoint)
                // - It's tracked in ProjectRequestOwnerHistory with OwnerRole = HeadReviewerConstants.HEAD_REVIEWER_ROLE_NAME
                // - This is NOT the same as the config role "Head of Product Management" (HEAD_PRODUCT_MGMT)
                // - The config role can be assigned as a reviewer, but Head Reviewer is the workflow owner
                await _ownerHistoryService.TrackOwnerChangeAsync(
                    id,
                    newOwnerId,
                    newOwnerName,
                    HeadReviewerConstants.HEAD_REVIEWER_ROLE_NAME, // This identifies the Head Reviewer (workflow owner)
                    currentUserId
                );

                // Reflect assignment on the request for quick lookup
                var req = await _context.ProjectRequests.FirstOrDefaultAsync(r => r.Id == id);
                if (req != null)
                {
                    req.AssignedTo = newOwnerName ?? newOwnerId;
                    req.LastUpdatedBy = currentUserId;
                    req.LastUpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                // Notify the new head (default notify unless explicitly disabled)
                try
                {
                    if (dto?.Notify != false)
                    {
                        var title = "Assigned as Head Reviewer";
                        var message = req != null
                            ? $"You have been assigned as Head Reviewer for '{req.RequestTitle}' (#{req.RequestID})."
                            : $"You have been assigned as Head Reviewer for request #{id}.";
                        await _notificationService.SendNotificationAsync(newOwnerId!, title, message, "HeadAssignment", "ProjectRequest", id);
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Failed to send head assignment notification for request {RequestId}", id);
                }

                return Ok(new
                {
                    success = true,
                    message = "Head reviewer assigned successfully",
                    headReviewerId = newOwnerId,
                    headReviewerName = newOwnerName,
                    notified = dto?.Notify == true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning head reviewer for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while assigning head reviewer" });
            }
        }

        // Get request IDs where current user is the active head/owner
        [HttpGet("head-assignments/me")]
        public async Task<ActionResult> GetHeadAssignmentsForMe()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUsername = User.Identity?.Name ?? currentUserId;
                
                // ✅ FIXED: Use ToLower() for case-insensitive comparison (EF Core can translate this to SQL)
                // Head Reviewer is tracked in OwnerHistory with OwnerRole = "Head of Product Management" or "Head Reviewer"
                var requestIds = await _context.ProjectRequestOwnerHistories
                    .Where(h => h.Status == "Active" && 
                               (h.OwnerID == currentUserId || h.OwnerID == currentUsername || 
                                h.OwnerName == currentUserId || h.OwnerName == currentUsername))
                    .ToListAsync(); // Load to memory first for case-insensitive filtering
                
                // Filter in memory for case-insensitive role matching
                // Match Head Reviewer role (workflow owner) - stored as "Head of Product Management" or "Head Reviewer"
                var headRequestIds = requestIds
                    .Where(h => h.OwnerRole.ToLower().Contains("head") || 
                               h.OwnerRole.ToLower().Contains("product management") ||
                               h.OwnerRole == HeadReviewerConstants.HEAD_REVIEWER_ROLE_NAME ||
                               h.OwnerRole == HeadReviewerConstants.HEAD_REVIEWER_ROLE_NAME_ALT)
                    .Select(h => h.ProjectRequestId)
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Found {Count} head assignments for user {UserId}", headRequestIds.Count, currentUserId);
                return Ok(new { requestIds = headRequestIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting head assignments for current user");
                return StatusCode(500, new { message = "An error occurred while fetching head assignments" });
            }
        }

        // Expose owner history for a request (structured response at later definition)

        // SUBMIT EVALUATION SCORES
        [HttpPost("{id}/evaluate")]
        public async Task<ActionResult<ProjectRequestDto>> SubmitEvaluation(int id, [FromBody] SubmitEvaluationDto evaluationDto)
        {
            try
            {
                _logger.LogInformation(" Submitting evaluation for request {RequestId}", id);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // TODO: Get from authentication context
                var updatedRequest = await _projectRequestService.SubmitEvaluationAsync(id, evaluationDto, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Evaluation submitted successfully",
                    request = updatedRequest,
                    calculatedScore = updatedRequest.TotalScore
                });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = $"Project request with ID {id} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting evaluation for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while submitting the evaluation" });
            }
        }

        // APPROVE A REQUEST
        [HttpPut("{id}/approve")]
        public async Task<ActionResult> Approve(int id, [FromBody] Model.Dto.ProjectRequestsDto.Requests.ApproveRequestDto approveDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _projectRequestService.ApproveAsync(id, approveDto.Remarks, currentUserId, approveDto.MinimumScoreThreshold);

                if (!result)
                    return BadRequest(new { message = $"Cannot approve request {id}. It may not meet approval criteria." });

                return Ok(new
                {
                    success = true,
                    message = "Request approved successfully",
                    approvedBy = currentUserId,
                    remarks = approveDto.Remarks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while approving the request" });
            }
        }
        // REJECT A REQUEST
        [HttpPut("{id}/reject")]
        public async Task<ActionResult> Reject(int id, [FromBody] Model.Dto.ProjectRequestsDto.Requests.RejectRequestDto rejectDto)
        {
            try
            {
                // ✅ FIXED: Validate model state and provide clear error messages
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Validation failed for reject request {RequestId}: {Errors}", id, string.Join(", ", errors));
                    return BadRequest(new { 
                        message = "Validation failed", 
                        errors = errors.ToList() 
                    });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _projectRequestService.RejectAsync(id, rejectDto?.Remarks ?? string.Empty, rejectDto.Reason, currentUserId);

                if (!result)
                    return BadRequest(new { message = $"Cannot reject request {id}." });

                return Ok(new
                {
                    success = true,
                    message = "Request rejected successfully",
                    rejectedBy = currentUserId,
                    reason = rejectDto.Reason.ToString(),
                    remarks = rejectDto?.Remarks ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while rejecting the request", error = ex.Message });
            }
        }

        [HttpGet("{id}/can-approve")]
        public async Task<ActionResult> CanApprove(int id)
        {
            try
            {
                _logger.LogInformation("Checking approval eligibility for request {RequestId}", id);

                // First, check if request exists
                var request = await _projectRequestService.GetByIdAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = $"Request {id} not found" });
                }

                // Check approval eligibility
                var canApprove = await _projectRequestService.CanBeApprovedAsync(id);

                // Get detailed evaluation info
                var evaluations = await _context.ProjectRequestEvaluations
                    .Where(e => e.ProjectRequestId == id)
                    .ToListAsync();

                var submittedCount = evaluations.Count(e => e.EvaluationStatus == EvaluationStatus.Submitted);
                var hasScores = evaluations.Any(e =>
                    e.StrategicAlignmentScore.HasValue &&
                    e.FeasibilityScore.HasValue &&
                    e.BusinessValueScore.HasValue &&
                    e.TechnicalComplexityScore.HasValue);

                // Get attachment compliance info
                var attachmentCompliance = await _attachmentComplianceService.ValidateAsync(id);

                // Get status name safely - Status is a ConfigInfoDto object
                var statusName = request.Status?.Name ?? "Unknown";

                return Ok(new
                {
                    success = true,
                    canApprove = canApprove,
                    requestDetails = new
                    {
                        id = request.Id,
                        title = request.RequestTitle,
                        status = statusName,
                        totalScore = request.TotalScore,
                        assignedTo = request.AssignedTo
                    },
                    evaluationDetails = new
                    {
                        totalEvaluators = evaluations.Count,
                        submittedEvaluations = submittedCount,
                        hasScores = hasScores,
                        allEvaluatorsSubmitted = evaluations.Count == submittedCount && evaluations.Count > 0
                    },
                    attachmentCompliance = new
                    {
                        isCompliant = attachmentCompliance.IsCompliant,
                        missingCategories = attachmentCompliance.MissingCategories
                    },
                    requirements = new
                    {
                        needsScores = true,
                        minimumScore = 6.0m,
                        needsUnderEvaluationStatus = true,
                        needsAssignedEvaluatorsSubmitted = true, // Changed from "all" to "assigned"
                        needsAttachments = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking approval eligibility for request {RequestId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking approval eligibility",
                    error = ex.Message,
                    detailedError = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("add-approval-ready-stage")]
        public async Task<ActionResult> AddApprovalReadyStage()
        {
            try
            {
                var result = await _configurationService.AddApprovalReadyStageAsync();

                if (result)
                {
                    return Ok(new { success = true, message = "Approval Ready stage added successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to add Approval Ready stage" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Approval Ready stage");
                return StatusCode(500, new { success = false, message = "Error adding stage" });
            }
        }

        [HttpGet("approval-dashboard")]
        public async Task<ActionResult> GetApprovalDashboard()
        {
            try
            {
                var dashboard = await _projectRequestService.GetApprovalDashboardAsync();

                return Ok(new
                {
                    success = true,
                    dashboard = dashboard,
                    summary = new
                    {
                        totalReady = dashboard.TotalReadyForApproval,
                        highPriority = dashboard.HighPriorityCount,
                        averageScore = Math.Round(dashboard.AverageScore, 2),
                        autoApproveCount = dashboard.Requests.Count(r => r.CanAutoApprove)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval dashboard");
                return StatusCode(500, new { message = "Error retrieving approval dashboard" });
            }
        }

        [HttpGet("ready-for-approval")]
        public async Task<ActionResult> GetReadyForApproval()
        {
            try
            {
                var requests = await _projectRequestService.GetRequestsReadyForApprovalAsync();

                return Ok(new
                {
                    success = true,
                    requests = requests,
                    count = requests.Count,
                    highScoreCount = requests.Count(r => r.TotalScore >= 8.0m),
                    mediumScoreCount = requests.Count(r => r.TotalScore >= 6.0m && r.TotalScore < 8.0m),
                    lowScoreCount = requests.Count(r => r.TotalScore < 6.0m)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests ready for approval");
                return StatusCode(500, new { message = "Error retrieving approval-ready requests" });
            }
        }

        [HttpPost("{id}/quick-approve")]
        public async Task<ActionResult> QuickApprove(int id, [FromBody] QuickApproveDto approveDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Check if request is in Approval Ready stage
                var request = await _context.ProjectRequests
                    .Include(r => r.WorkflowStageConfig)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                    return NotFound(new { message = $"Request {id} not found" });

                if (request.WorkflowStageConfig?.Code != "APPROVAL_READY")
                {
                    return BadRequest(new
                    {
                        message = "Request is not in Approval Ready stage",
                        currentStage = request.WorkflowStageConfig?.Name
                    });
                }

                // Auto-approve if high score, otherwise use normal approval
                if (request.TotalScore >= 8.0m && approveDto.AutoApproveHighScores)
                {
                    var result = await _projectRequestService.ApproveAsync(id,
                        $"Auto-approved: High score ({request.TotalScore}) - {approveDto.Remarks}",
                        currentUserId);

                    if (result)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Request auto-approved (high score)",
                            autoApproved = true,
                            score = request.TotalScore,
                            approvedBy = currentUserId
                        });
                    }
                }

                // Standard approval
                var approvalResult = await _projectRequestService.ApproveAsync(id, approveDto.Remarks, currentUserId);

                if (!approvalResult)
                {
                    return BadRequest(new { message = "Approval failed" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Request approved successfully",
                    autoApproved = false,
                    approvedBy = currentUserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error quick-approving request {RequestId}", id);
                return StatusCode(500, new { message = "Error during quick approval" });
            }
        }

        [HttpGet("approval-analytics")]
        public async Task<ActionResult> GetApprovalAnalytics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var analytics = await _approvalAnalyticsService.GetApprovalAnalyticsAsync(startDate, endDate);

                // ✅ FIXED: Proper type handling for period calculation
                string periodDescription = "all";
                if (startDate.HasValue && endDate.HasValue)
                {
                    var days = (endDate.Value - startDate.Value).TotalDays;
                    periodDescription = $"{days} days";
                }

                return Ok(new
                {
                    success = true,
                    analytics = analytics,
                    period = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.ToString("yyyy-MM-dd"),
                        days = periodDescription // ✅ FIXED: Use string variable
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval analytics");
                return StatusCode(500, new { message = "Error retrieving approval analytics" });
            }
        }

        [HttpGet("approval-trends")]
        public async Task<ActionResult> GetApprovalTrends([FromQuery] int days = 30)
        {
            try
            {
                var trends = await _approvalAnalyticsService.GetApprovalTrendsAsync(days);

                return Ok(new
                {
                    success = true,
                    trends = trends,
                    periodDays = days
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval trends");
                return StatusCode(500, new { message = "Error retrieving approval trends" });
            }
        }

        [HttpGet("score-analysis")]
        public async Task<ActionResult> GetScoreAnalysis()
        {
            try
            {
                var analysis = await _approvalAnalyticsService.GetScoreAnalysisAsync();

                return Ok(new
                {
                    success = true,
                    analysis = analysis
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting score analysis");
                return StatusCode(500, new { message = "Error retrieving score analysis" });
            }
        }

        [HttpPost("bulk-approve")]
        public async Task<ActionResult> BulkApprove([FromBody] BulkApproveDto bulkApproveDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (bulkApproveDto.RequestIds == null || !bulkApproveDto.RequestIds.Any())
                {
                    return BadRequest(new { message = "No request IDs provided" });
                }

                var result = await _approvalAnalyticsService.BulkApproveAsync(
                    bulkApproveDto.RequestIds,
                    bulkApproveDto.Remarks,
                    currentUserId);

                return Ok(new
                {
                    success = true,
                    result = result,
                    summary = new
                    {
                        totalProcessed = bulkApproveDto.RequestIds.Count,
                        successful = result.SuccessfulApprovals,
                        failed = result.FailedApprovals,
                        successRate = bulkApproveDto.RequestIds.Count > 0 ?
                            (decimal)result.SuccessfulApprovals / bulkApproveDto.RequestIds.Count * 100 : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk approval");
                return StatusCode(500, new { message = "Error during bulk approval" });
            }
        }

        // QUERY: GET REQUESTS ASSIGNED TO CURRENT USER
        [HttpGet("assigned-to-me")]
        public async Task<ActionResult<List<ProjectRequestDto>>> GetAssignedToMe()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // TODO: Get from authentication context
                var requests = await _projectRequestService.GetAssignedToMeAsync(currentUserId);

                return Ok(new
                {
                    success = true,
                    assignedTo = currentUserId,
                    count = requests.Count,
                    requests = requests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests assigned to current user");
                return StatusCode(500, new { message = "An error occurred while retrieving assigned requests" });
            }
        }

        [HttpGet("primary-assignee")]
        public async Task<ActionResult> GetPrimaryAssignee(int requestId)
        {
            //var curreUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var requests = await _projectRequestService.GetPrimaryAssigneeInfoAsync(requestId);
            return Ok(requests);
        }

        // QUERY: GET REQUESTS NEEDING EVALUATION
        [HttpGet("needing-evaluation")]
        public async Task<ActionResult<List<ProjectRequestDto>>> GetNeedingEvaluation()
        {
            try
            {
                var requests = await _projectRequestService.GetRequestsNeedingEvaluationAsync();

                return Ok(new
                {
                    success = true,
                    count = requests.Count,
                    requests = requests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests needing evaluation");
                return StatusCode(500, new { message = "An error occurred while retrieving requests needing evaluation" });
            }
        }

        // QUERY: GET REQUESTS PENDING APPROVAL
        [HttpGet("pending-approval")]
        public async Task<ActionResult<List<ProjectRequestDto>>> GetPendingApproval()
        {
            try
            {
                var requests = await _projectRequestService.GetPendingApprovalAsync();

                return Ok(new
                {
                    success = true,
                    count = requests.Count,
                    requests = requests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests pending approval");
                return StatusCode(500, new { message = "An error occurred while retrieving requests pending approval" });
            }
        }

        // GET WORKFLOW HISTORY FOR A REQUEST
        [HttpGet("{id}/workflow-history")]
        public async Task<ActionResult> GetWorkflowHistory(int id)
        {
            try
            {
                var statusHistory = await _projectRequestService.GetStatusHistoryAsync(id);
                var workflowHistory = await _projectRequestService.GetWorkflowHistoryAsync(id);

                return Ok(new
                {
                    success = true,
                    requestId = id,
                    statusHistory = statusHistory,
                    workflowHistory = workflowHistory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting workflow history for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving workflow history" });
            }
        }

        [HttpGet("{id}/owner-history")]
        public async Task<ActionResult> GetOwnerHistory(int id)
        {
            try
            {
                var history = await _ownerHistoryService.GetHistoryAsync(id);
                return Ok(new
                {
                    success = true,
                    history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting owner history for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving owner history" });
            }
        }

        [HttpPost("{id}/owner-history")]
        public async Task<ActionResult> AddOwnerHistory(int id, [FromBody] CreateOwnerHistoryDto dto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";
                var entry = await _ownerHistoryService.AddAsync(id, dto, currentUserId);
                return Ok(new
                {
                    success = true,
                    message = "Owner history entry created",
                    entry
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating owner history for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while creating owner history" });
            }
        }

        // CHECK IF STATUS TRANSITION IS ALLOWED
        [HttpGet("{id}/can-transition-to/{statusConfigId}")]
        public async Task<ActionResult> CanTransitionToStatus(int id, int statusConfigId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // TODO: Get from authentication context
                var canTransition = await _workflowService.CanTransitionToStatusAsync(id, statusConfigId, currentUserId);
                var validationErrors = await _workflowService.ValidateTransitionAsync(id, statusConfigId, currentUserId);

                return Ok(new
                {
                    success = true,
                    canTransition = canTransition,
                    validationErrors = validationErrors,
                    message = canTransition ? "Transition is allowed" : "Transition is not allowed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking transition for request {RequestId} to status {StatusId}", id, statusConfigId);
                return StatusCode(500, new { message = "An error occurred while checking transition" });
            }


        }

        [HttpPut("{id}/set-primary-assignee")]
        public async Task<ActionResult> SetPrimaryAssignee(int id, [FromBody] SetPrimaryAssigneeDto dto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _projectRequestService.SetPrimaryAssigneeAsync(id, dto.AssigneeId, currentUserId);

                if (!result)
                    return BadRequest(new { message = "Failed to set primary assignee. Assignment may not exist." });

                return Ok(new
                {
                    success = true,
                    message = $"Successfully set {dto.AssigneeId} as primary assignee",
                    requestId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary assignee for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while setting primary assignee" });
            }
        }

        [HttpPut("{id}/set-primary-evaluator")]
        public async Task<ActionResult> SetPrimaryEvaluator(int id, [FromBody] SetPrimaryEvaluatorDto dto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Use the strategy directly for evaluator-specific method
                var result = await _assignmentStrategy.SetPrimaryEvaluatorAsync(id, dto.EvaluatorId, currentUserId);

                if (!result)
                    return BadRequest(new { message = "Failed to set primary evaluator. Assignment may not exist or user may not be an evaluator." });

                // Update the main request
                var request = await _context.ProjectRequests.FindAsync(id);
                if (request != null)
                {
                    request.EvaluatorID = dto.EvaluatorId;
                    request.LastUpdatedDate = DateTime.UtcNow;
                    request.LastUpdatedBy = currentUserId;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"Successfully set {dto.EvaluatorId} as primary evaluator",
                    requestId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary evaluator for request {RequestId}", id);
                return StatusCode(500, new { message = "An error occurred while setting primary evaluator" });
            }
        }

        // ✅ ADD THIS ENDPOINT
        //[HttpPut("assignments/{assignmentId}/complete")]
        //public async Task<ActionResult> CompleteAssignment(int assignmentId)
        //{
        //    try
        //    {
        //        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var result = await _projectRequestService.CompleteAssignmentAsync(assignmentId, currentUserId);

        //        if (!result)
        //            return NotFound(new { message = $"Assignment {assignmentId} not found" });

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Assignment completed successfully",
        //            completedAt = DateTime.UtcNow
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error completing assignment {AssignmentId}", assignmentId);
        //        return StatusCode(500, new { message = "An error occurred while completing the assignment" });
        //    }
        //}

        [HttpPut("assignments/{assignmentId}/complete")]
        public async Task<ActionResult> CompleteAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var result = await _projectRequestService.CompleteAssignmentAsync(assignmentId, currentUserId);

                if (!result)
                    return NotFound(new
                    {
                        success = false,
                        message = $"Assignment {assignmentId} not found or already completed"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Assignment completed successfully",
                    completedAt = DateTime.UtcNow,
                    assignmentId = assignmentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing assignment {AssignmentId}", assignmentId);
                return StatusCode(500, new { message = "Error completing assignment" });
            }
        }

        [HttpPut("assignments/{assignmentId}/revoke")]
        public async Task<ActionResult> RevokeAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var result = await _projectRequestService.RevokeAssignmentAsync(assignmentId, currentUserId);

                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Assignment {assignmentId} not found or already completed/revoked"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Assignment revoked successfully",
                    revokedAt = DateTime.UtcNow,
                    assignmentId = assignmentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking assignment {AssignmentId}", assignmentId);
                return StatusCode(500, new { message = "Error revoking assignment" });
            }
        }

        [HttpGet("{id}/debug-primary-evaluator")]
        public async Task<ActionResult> DebugPrimaryEvaluator(int id)
        {
            try
            {
                // Get primary evaluator from assignment table
                var primaryAssignment = await _context.ProjectRequestAssignments
                    .FirstOrDefaultAsync(a => a.ProjectRequestId == id &&
                                             a.AssigneeRole == "Evaluator" &&
                                             a.IsPrimaryEvaluator &&
                                             !a.CompletedDate.HasValue);

                // Get primary evaluator from evaluation table  
                var primaryEvaluation = await _context.ProjectRequestEvaluations
                    .FirstOrDefaultAsync(e => e.ProjectRequestId == id && e.IsPrimaryEvaluation);

                return Ok(new
                {
                    success = true,
                    requestId = id,
                    assignmentPrimary = primaryAssignment != null ? new
                    {
                        assigneeId = primaryAssignment.AssigneeId,
                        isPrimaryEvaluator = primaryAssignment.IsPrimaryEvaluator
                    } : null,
                    evaluationPrimary = primaryEvaluation != null ? new
                    {
                        reviewerId = primaryEvaluation.ReviewerId,
                        isPrimaryEvaluation = primaryEvaluation.IsPrimaryEvaluation
                    } : null,
                    synchronized = primaryAssignment?.AssigneeId == primaryEvaluation?.ReviewerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging primary evaluator for request {RequestId}", id);
                return StatusCode(500, new { message = "Error debugging primary evaluator" });
            }
        }

        [HttpGet("debug-request/{id}")]
        public async Task<ActionResult> DebugRequest(int id)
        {
            try
            {
                var request = await _context.ProjectRequests
                    .Include(r => r.StatusConfig)
                    .Include(r => r.WorkflowStageConfig)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                    return NotFound(new { message = $"Request {id} not found" });

                return Ok(new
                {
                    success = true,
                    request = new
                    {
                        id = request.Id,
                        title = request.RequestTitle,
                        status = request.StatusConfig?.Name,
                        workflowStage = request.WorkflowStageConfig?.Name,
                        assignedTo = request.AssignedTo,
                        assignedTeam = request.AssignedTeam
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging request {RequestId}", id);
                return StatusCode(500, new { message = "Error debugging request" });
            }
        }

        // Add these endpoints to your ProjectRequestsController

        [HttpGet("{id}/assignments")]
        public async Task<ActionResult> GetAssignments(int id)
        {
            try
            {
                var assignments = await _projectRequestService.GetAssignmentAsync(id);

                return Ok(new
                {
                    success = true,
                    requestId = id,
                    assignments = assignments.Select(a => new {
                        Id = a.Id,
                        AssigneeId = a.AssigneeId,
                        AssigneeRole = a.AssigneeRole,
                        ReviewerType = a.ReviewerType,
                        AssignedDate = a.AssignedDate,
                        CompletedDate = a.CompletedDate,
                        IsPrimary = a.IsPrimary,
                        IsPrimaryEvaluator = a.IsPrimaryEvaluator,
                        AssignmentNotes = a.AssignmentNotes
                    }),
                    count = assignments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for request {RequestId}", id);
                return StatusCode(500, new { message = "Error retrieving assignments" });
            }
        }

        

        [HttpGet("workflow-stages")]
        public async Task<ActionResult> GetWorkflowStages()
        {
            try
            {
                var stages = await _context.WorkflowStageConfigs
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.SortOrder)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    stages = stages.Select(s => new {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        Description = s.Description,
                        SortOrder = s.SortOrder
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow stages");
                return StatusCode(500, new { message = "Error retrieving workflow stages" });
            }
        }

        [HttpGet("by-workflow-stage/{stageId}")]
        public async Task<ActionResult> GetByWorkflowStage(int stageId)
        {
            try
            {
                var requests = await _projectRequestService.GetByWorkflowStageAsync(stageId);

                return Ok(new
                {
                    success = true,
                    stageId = stageId,
                    requests = requests,
                    count = requests.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests for workflow stage {StageId}", stageId);
                return StatusCode(500, new { message = "Error retrieving requests by workflow stage" });
            }
        }

        [HttpGet("{id}/debug-assignment-state")]
public async Task<ActionResult> DebugAssignmentState(int id)
{
    try
    {
        var request = await _context.ProjectRequests
            .Include(r => r.Assignments)
            .Include(r => r.StatusConfig)
            .Include(r => r.WorkflowStageConfig)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            return NotFound(new { message = $"Request {id} not found" });

        var assignments = request.Assignments.ToList();
        var evaluations = await _context.ProjectRequestEvaluations
            .Where(e => e.ProjectRequestId == id)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            request = new {
                id = request.Id,
                assignedTo = request.AssignedTo,
                assignedTeam = request.AssignedTeam,
                evaluatorId = request.EvaluatorID,
                status = request.StatusConfig?.Name,
                workflowStage = request.WorkflowStageConfig?.Name
            },
            assignments = assignments.Select(a => new {
                id = a.Id,
                assigneeId = a.AssigneeId,
                role = a.AssigneeRole,
                isPrimary = a.IsPrimary,
                isPrimaryEvaluator = a.IsPrimaryEvaluator,
                completed = a.CompletedDate.HasValue
            }),
            evaluations = evaluations.Select(e => new {
                id = e.Id,
                reviewerId = e.ReviewerId,
                isPrimaryEvaluation = e.IsPrimaryEvaluation,
                status = e.EvaluationStatus.ToString()
            }),
            summary = new {
                totalAssignments = assignments.Count,
                totalEvaluations = evaluations.Count,
                hasPrimaryAssignee = !string.IsNullOrEmpty(request.AssignedTo),
                hasEvaluator = !string.IsNullOrEmpty(request.EvaluatorID)
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error debugging assignment state for request {RequestId}", id);
        return StatusCode(500, new { message = "Error debugging assignment state" });
    }
}
    }




}