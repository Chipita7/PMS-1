using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.AuditDto;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Requests;
using ProjectManagementSystem1.Model.Dto.WorkflowDto.Responses;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Dto.Workflow.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Attachments;
using ProjectManagementSystem1.Services.Configuration;
using ProjectManagementSystem1.Services.ReviewTasks;
using ProjectManagementSystem1.Services.UserService;
using ProjectManagementSystem1.Services.WorkflowService;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public class ProjectRequestService : IProjectRequestService
    {
        private readonly AppDbContext _context;
        private readonly IConfigurationService _configService;
        private readonly ILogger<ProjectRequestService> _logger;
        private readonly IUserService _userService;
        private readonly IWorkflowService _workflowService;
        private readonly IDynamicAssignmentStrategy _assignmentStrategy;
        private readonly IReviewTaskService _reviewTaskService;
        private readonly IProjectRequestAuditService _auditService;
        private readonly IProjectRequestOwnerHistoryService _ownerHistoryService;
        private readonly IAttachmentComplianceService _attachmentComplianceService;


        public ProjectRequestService(AppDbContext context, IConfigurationService configService, ILogger<ProjectRequestService> logger
            , IUserService userService, IWorkflowService workflowService, IDynamicAssignmentStrategy assignmentStrategy,
            IReviewTaskService reviewTaskService, IProjectRequestAuditService auditService,
            IProjectRequestOwnerHistoryService ownerHistoryService,
            IAttachmentComplianceService attachmentComplianceService)
        {
            _context = context;
            _configService = configService;
            _logger = logger;
            _userService = userService;
            _workflowService = workflowService;
            _assignmentStrategy = assignmentStrategy;
            _reviewTaskService = reviewTaskService;
            _auditService = auditService;
            _ownerHistoryService = ownerHistoryService;
            _attachmentComplianceService = attachmentComplianceService;
        }

        private async Task<bool> SetStatusAndStageAsync(int requestId, string statusCode, string? stageCode, string remarks, string currentUserId)
        {
            var statusConfig = await _configService.GetStatusByCodeAsync(statusCode);
            if (statusConfig == null)
            {
                _logger.LogWarning("Status code {StatusCode} not found in configuration", statusCode);
                return false;
            }

            var transitionResult = await _workflowService.TransitionToStatusAsync(
                requestId,
                statusConfig.Id,
                currentUserId,
                remarks);

            if (!transitionResult.Success)
            {
                _logger.LogWarning("Failed to transition request {RequestId} to status {Status}", requestId, statusCode);
                return false;
            }

            // Update additional fields based on status
            var request = await _context.ProjectRequests.FindAsync(requestId);
            if (request != null)
            {
                if (statusConfig.Code.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    request.ApprovalDate = DateTime.UtcNow;
                }

                if (statusConfig.Code.Equals("BACKLOGGED", StringComparison.OrdinalIgnoreCase))
                {
                    request.AssignedTo = null;
                    request.AssignedTeam = null;
                }

                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = currentUserId;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(stageCode))
            {
                var stageConfig = await _configService.GetWorkflowStageByCodeAsync(stageCode);
                if (stageConfig != null)
                {
                    await _workflowService.UpdateWorkflowStageAsync(requestId, stageConfig.Id, currentUserId);
                }
                else
                {
                    _logger.LogWarning("Workflow stage {StageCode} not found while updating request {RequestId}", stageCode, requestId);
                }
            }

            return true;
        }

        public async Task<ProjectRequestDto> CreateAsync(CreateProjectRequestDto createDto, string currentUserId)
        {
            try
            {
                // Generate RequestID
                var requestId = await GenerateRequestIdAsync();

                // Get default status (Submitted)
                var defaultStatus = (await _configService.GetActiveStatusesAsync())
                    .FirstOrDefault(s => s.Code == "SUBMITTED");

                if (defaultStatus == null)
                    throw new InvalidOperationException("Default status not found");

                var resolvedRequesterName = !string.IsNullOrWhiteSpace(createDto.RequestedByName)
                    ? createDto.RequestedByName
                    : (await _userService.GetUserByEmployeeIdAsync(currentUserId))?.FullName ?? currentUserId;

                var resolvedBusinessSector = !string.IsNullOrWhiteSpace(createDto.BusinessSector)
                    ? createDto.BusinessSector
                    : "Digital Banking";

                var resolvedBusinessDivision = !string.IsNullOrWhiteSpace(createDto.BusinessDivision)
                    ? createDto.BusinessDivision
                    : "Technology";

                var resolvedBusinessDepartment = !string.IsNullOrWhiteSpace(createDto.BusinessDepartment)
                    ? createDto.BusinessDepartment
                    : "Digital Factory";

                var resolvedOrganization = !string.IsNullOrWhiteSpace(createDto.OrganizationName)
                    ? createDto.OrganizationName
                    : "Commercial Bank of Ethiopia";

                var projectRequest = new ProjectRequest
                {
                    RequestID = requestId,
                    RequestTitle = createDto.RequestTitle,
                    RequestDescription = createDto.RequestDescription,
                    ReferenceNo = createDto.ReferenceNo,

                    // Config IDs from DTO
                    RequestTypeConfigId = createDto.RequestTypeConfigId,
                    RequestCategoryConfigId = createDto.RequestCategoryConfigId,
                    ServiceCategoryConfigId = createDto.ServiceCategoryConfigId,
                    ProductCategoryConfigId = createDto.ProductCategoryConfigId,
                    PriorityConfigId = createDto.PriorityConfigId,
                    BusinessImpactConfigId = createDto.BusinessImpactConfigId,
                    RequestUrgencyConfigId = createDto.RequestUrgencyConfigId,
                    StatusConfigId = defaultStatus.Id,

                    // Additional fields
                    StrategicAlignment = createDto.StrategicAlignment,
                    EstimatedCost = createDto.EstimatedCost,
                    EstimatedBenefit = createDto.EstimatedBenefit,
                    BenefitCaptureDuration = createDto.BenefitCaptureDuration,
                    RequestedDeliveryDate = createDto.RequestedDeliveryDate,
                    RiskLevelConfigId = createDto.RiskLevelConfigId,
                    ComplexityLevelConfigId = createDto.ComplexityLevelConfigId,


                    // Requestor info (would come from current user context)
                    RequestedBy = currentUserId,
                    RequestedByName = resolvedRequesterName,

                    BusinessSector = resolvedBusinessSector,
                    BusinessDivision = resolvedBusinessDivision,
                    BusinessDepartment = resolvedBusinessDepartment,
                    OrganizationName = resolvedOrganization,
                    PrimaryContactEmail = createDto.PrimaryContactEmail,
                    SecondaryContactEmail = createDto.SecondaryContactEmail,
                    PrimaryContactPhone = createDto.PrimaryContactPhone,
                    SecondaryContactPhone = createDto.SecondaryContactPhone,
                    // ✅ FIXED: AssignedTo should only hold creator/primary owner, not reviewers
                    // Set to creator initially (per user requirement)
                    AssignedTo = currentUserId,
                    
                    // Audit
                    CreatedBy = currentUserId,
                    LastUpdatedBy = currentUserId
                };

                if (createDto.StrategicAlignmentConfigId.HasValue)
                {
                    var alignment = await _configService.GetStrategicAlignmentByIdAsync(createDto.StrategicAlignmentConfigId.Value);
                    if (alignment != null)
                    {
                        projectRequest.StrategicAlignmentConfigId = alignment.Id;
                        projectRequest.StrategicAlignment = alignment.Name;
                    }
                }

                _context.ProjectRequests.Add(projectRequest);
                await _context.SaveChangesAsync();

                await _auditService.LogCreationAsync(projectRequest.Id, currentUserId, new
                {
                    Title = createDto.RequestTitle,
                    Description = createDto.RequestDescription,
                    ReferenceNo = createDto.ReferenceNo,
                    InitialStatus = "SUBMITTED"
                });

                await _ownerHistoryService.TrackOwnerChangeAsync(
                    projectRequest.Id,
                    currentUserId,
                    resolvedRequesterName,
                    "Requestor",
                    currentUserId);

                var createdRequest = await GetProjectRequestWithIncludes()
                .FirstOrDefaultAsync(pr => pr.Id == projectRequest.Id);

                return await MapToProjectRequestDto(createdRequest!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project request");
                throw;
            }
        }

        private IQueryable<ProjectRequest> GetProjectRequestWithIncludes()
        {
            return _context.ProjectRequests
                .Include(pr => pr.RequestTypeConfig)
                .Include(pr => pr.PriorityConfig)
                .Include(pr => pr.StatusConfig)
                .Include(pr => pr.RequestCategoryConfig)
                .Include(pr => pr.ServiceCategoryConfig)
                .Include(pr => pr.ProductCategoryConfig)
                .Include(pr => pr.BusinessImpactConfig)
                .Include(pr => pr.RequestUrgencyConfig)
                .Include(pr => pr.WorkflowStageConfig)
                .Include(pr => pr.RiskLevelConfig)
                .Include(pr => pr.ComplexityLevelConfig)
                .Include(pr => pr.StrategicAlignmentConfig);
        }


        public async Task<ProjectRequestDetailDto?> GetByIdAsync(int id)
        {
            //var projectRequest = await _context.ProjectRequests
            //    .Include(pr => pr.StatusConfig)
            //    .Include(pr => pr.PriorityConfig)
            //    .Include(pr => pr.RequestTypeConfig)
            //    .Include(pr => pr.RequestCategoryConfig)
            //    .Include(pr => pr.ServiceCategoryConfig)
            //    .Include(pr => pr.ProductCategoryConfig)
            //    .Include(pr => pr.BusinessImpactConfig)
            //    .Include(pr => pr.RequestUrgencyConfig)
            //    .Include(pr => pr.WorkflowStageConfig)
            //    .Include(pr => pr.RiskLevelConfig)
            //    .Include(pr => pr.ComplexityLevelConfig)
            //    .FirstOrDefaultAsync(pr => pr.Id == id);

            var projectRequest = await GetProjectRequestWithIncludes()
        .FirstOrDefaultAsync(pr => pr.Id == id);

            return projectRequest != null ? MapToProjectRequestDetailDto(projectRequest) : null;
        }

        public async Task<List<ProjectRequestDto>> GetAllAsync()
        {
            var projectRequests = await _context.ProjectRequests
                .Include(pr => pr.StatusConfig)
                .Include(pr => pr.PriorityConfig)
                .Include(pr => pr.RequestTypeConfig)
                .OrderByDescending(pr => pr.CreatedDate)
                .ToListAsync();

            var dtos = new List<ProjectRequestDto>();
            foreach (var pr in projectRequests)
            {
                dtos.Add(await MapToProjectRequestDto(pr));
            }

            return dtos;
        }


        private async Task<string> GenerateRequestIdAsync()
        {
            var today = DateTime.UtcNow.Date;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sequence = await _context.RequestIdSequences
                    .FirstOrDefaultAsync(s => s.RequestDate == today);

                if (sequence == null)
                {
                    sequence = new RequestIdSequence
                    {
                        RequestDate = today,
                        Counter = 1
                    };
                    _context.RequestIdSequences.Add(sequence);
                }
                else
                {
                    if (sequence.Counter >= 99)
                    {
                        throw new InvalidOperationException("Maximum of 99 project requests per day reached. Please try again tomorrow.");
                    }

                    sequence.Counter += 1;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return $"PR{today:yyyyMMdd}{sequence.Counter:D2}";
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<ProjectRequestDto> MapToProjectRequestDto(ProjectRequest projectRequest)
        {
            // If navigation properties are not loaded, load them
            if (projectRequest.RequestTypeConfig == null ||
                projectRequest.PriorityConfig == null ||
                projectRequest.StatusConfig == null)
            {
                // Explicitly load the related entities
                await _context.Entry(projectRequest)
                    .Reference(pr => pr.RequestTypeConfig)
                    .LoadAsync();

                await _context.Entry(projectRequest)
                    .Reference(pr => pr.PriorityConfig)
                    .LoadAsync();

                await _context.Entry(projectRequest)
                    .Reference(pr => pr.StatusConfig)
                    .LoadAsync();
            }

            // ✅ FIXED: Get active head reviewer from owner history
            // Head Reviewer (workflow owner) is tracked with OwnerRole = "Head of Product Management"
            // This is different from the AssignmentRoleConfig role "Head of Product Management" (HEAD_PRODUCT_MGMT)
            var activeHeadEntries = await _context.ProjectRequestOwnerHistories
                .Where(h => h.ProjectRequestId == projectRequest.Id && h.Status == "Active")
                .ToListAsync(); // Load to memory for case-insensitive filtering
            
            var activeHead = activeHeadEntries
                .Where(h => h.OwnerRole.ToLower().Contains("head") || 
                           h.OwnerRole.ToLower().Contains("product management"))
                .OrderByDescending(h => h.StartDate)
                .FirstOrDefault();

            return new ProjectRequestDto
            {
                Id = projectRequest.Id,
                RequestID = projectRequest.RequestID,
                RequestTitle = projectRequest.RequestTitle,
                RequestDescription = projectRequest.RequestDescription,
                ReferenceNo = projectRequest.ReferenceNo,
                RequestType = projectRequest.RequestTypeConfig?.Name ?? "Unknown",
                Priority = projectRequest.PriorityConfig?.Name ?? "Unknown",
                PriorityColor = projectRequest.PriorityConfig?.Color,
                Status = projectRequest.StatusConfig?.Name ?? "Unknown",
                RequestedByName = projectRequest.RequestedByName,
                BusinessDepartment = projectRequest.BusinessDepartment,
                StrategicAlignment = projectRequest.StrategicAlignment,
                TimeToDeliveryDays = projectRequest.TimeToDeliveryDays,
                DaysUntilDelivery = projectRequest.DaysUntilDelivery,
                CreatedDate = projectRequest.CreatedDate,
                RequestDurationDays = projectRequest.RequestDurationDays,
                TotalScore = projectRequest.TotalScore,
                AssignedTo = projectRequest.AssignedTo,
                // ✅ FIXED: Include head reviewer information
                HeadReviewerId = activeHead?.OwnerID,
                HeadReviewerName = activeHead?.OwnerName,
                HeadAssignedAt = activeHead?.StartDate
            };
        }

        private ProjectRequestDetailDto MapToProjectRequestDetailDto(ProjectRequest projectRequest)
        {
            return new ProjectRequestDetailDto
            {
                Id = projectRequest.Id,
                RequestID = projectRequest.RequestID,
                RequestTitle = projectRequest.RequestTitle,
                RequestDescription = projectRequest.RequestDescription,
                ReferenceNo = projectRequest.ReferenceNo,

                // Map configs to DTOs with null checks
                RequestType = MapToConfigInfoDto(projectRequest.RequestTypeConfig),
                RequestCategory = MapToConfigInfoDto(projectRequest.RequestCategoryConfig),
                ServiceCategory = MapToConfigInfoDto(projectRequest.ServiceCategoryConfig),
                ProductCategory = MapToConfigInfoDto(projectRequest.ProductCategoryConfig),
                Priority = MapToConfigInfoDto(projectRequest.PriorityConfig),
                BusinessImpact = MapToConfigInfoDto(projectRequest.BusinessImpactConfig),
                RequestUrgency = MapToConfigInfoDto(projectRequest.RequestUrgencyConfig),
                Status = MapToConfigInfoDto(projectRequest.StatusConfig),
                WorkflowStage = projectRequest.WorkflowStageConfig != null ? MapToConfigInfoDto(projectRequest.WorkflowStageConfig) : null,
                RiskLevel = projectRequest.RiskLevelConfig != null ? MapToConfigInfoDto(projectRequest.RiskLevelConfig) : null,
                ComplexityLevel = projectRequest.ComplexityLevelConfig != null ? MapToConfigInfoDto(projectRequest.ComplexityLevelConfig) : null,
                StrategicAlignmentInfo = projectRequest.StrategicAlignmentConfig != null ? MapToConfigInfoDto(projectRequest.StrategicAlignmentConfig) : null,

                // Requestor info
                RequestedBy = projectRequest.RequestedBy,
                RequestedByName = projectRequest.RequestedByName,
                BusinessSector = projectRequest.BusinessSector,
                BusinessDivision = projectRequest.BusinessDivision,
                BusinessDepartment = projectRequest.BusinessDepartment,
                OrganizationName = projectRequest.OrganizationName,
                PrimaryContactEmail = projectRequest.PrimaryContactEmail,
                SecondaryContactEmail = projectRequest.SecondaryContactEmail,
                PrimaryContactPhone = projectRequest.PrimaryContactPhone,
                SecondaryContactPhone = projectRequest.SecondaryContactPhone,

                // Additional fields
                StrategicAlignment = projectRequest.StrategicAlignment,
                EstimatedCost = projectRequest.EstimatedCost,
                EstimatedBenefit = projectRequest.EstimatedBenefit,
                BenefitCaptureDuration = projectRequest.BenefitCaptureDuration,
                RequestedDeliveryDate = projectRequest.RequestedDeliveryDate,

                // Evaluation
                EvaluatorID = projectRequest.EvaluatorID,
                FeasibilityScore = projectRequest.FeasibilityScore,
                BusinessValueScore = projectRequest.BusinessValueScore,
                TechnicalComplexityScore = projectRequest.TechnicalComplexityScore,
                TotalScore = projectRequest.TotalScore,
                EvaluationRemarks = projectRequest.EvaluationRemarks,

                // Workflow
                AssignedTeam = projectRequest.AssignedTeam,
                AssignedTo = projectRequest.AssignedTo,

                // Audit
                CreatedDate = projectRequest.CreatedDate,
                LastUpdatedDate = projectRequest.LastUpdatedDate,
                CreatedBy = projectRequest.CreatedBy,
                ApprovalDate = projectRequest.ApprovalDate,

                // Computed
                RequestDurationDays = CalculateRequestDurationDays(projectRequest.CreatedDate),
                TimeToDeliveryDays = CalculateTimeToDeliveryDays(projectRequest.CreatedDate, projectRequest.RequestedDeliveryDate),
                DaysUntilDelivery = CalculateDaysUntilDelivery(projectRequest.RequestedDeliveryDate)
            };
        }

        private int CalculateRequestDurationDays(DateTime createdDate)
        {
            var timeSpan = DateTime.UtcNow - createdDate;
            var days = (int)Math.Ceiling(timeSpan.TotalDays);
            return Math.Max(1, days);
        }

        private int? CalculateTimeToDeliveryDays(DateTime createdDate, DateTime? requestedDeliveryDate)
        {
            if (!requestedDeliveryDate.HasValue) return null;

            var timeSpan = requestedDeliveryDate.Value - createdDate;
            return Math.Max(1, (int)Math.Ceiling(timeSpan.TotalDays));
        }

        private int? CalculateDaysUntilDelivery(DateTime? requestedDeliveryDate)
        {
            if (!requestedDeliveryDate.HasValue) return null;

            var timeSpan = requestedDeliveryDate.Value - DateTime.UtcNow;
            var days = (int)Math.Ceiling(timeSpan.TotalDays);
            return days < 0 ? 0 : days;
        }


        private ConfigInfoDto MapToConfigInfoDto(object? config)
        {
            if (config == null)
                return new ConfigInfoDto { Id = 0, Name = "Not Set", Code = null, Color = null };

            return config switch
            {
                StatusConfig status => new ConfigInfoDto { Id = status.Id, Name = status.Name, Code = status.Code },
                PriorityConfig priority => new ConfigInfoDto { Id = priority.Id, Name = priority.Name, Code = priority.Code, Color = priority.Color },
                RequestTypeConfig requestType => new ConfigInfoDto { Id = requestType.Id, Name = requestType.Name, Code = requestType.Code },
                RequestCategoryConfig requestCategory => new ConfigInfoDto { Id = requestCategory.Id, Name = requestCategory.Name, Code = requestCategory.Code },
                ServiceCategoryConfig serviceCategory => new ConfigInfoDto { Id = serviceCategory.Id, Name = serviceCategory.Name, Code = serviceCategory.Code },
                ProductCategoryConfig productCategory => new ConfigInfoDto { Id = productCategory.Id, Name = productCategory.Name, Code = productCategory.Code },
                ImpactUrgencyConfig impactUrgency => new ConfigInfoDto { Id = impactUrgency.Id, Name = impactUrgency.Name, Code = impactUrgency.Code, Color = impactUrgency.Color },
                WorkflowStageConfig workflowStage => new ConfigInfoDto { Id = workflowStage.Id, Name = workflowStage.Name, Code = workflowStage.Code },
                StrategicAlignmentConfig strategicAlignment => new ConfigInfoDto { Id = strategicAlignment.Id, Name = strategicAlignment.Name, Code = strategicAlignment.Code },
                _ => new ConfigInfoDto { Id = 0, Name = "Unknown Type", Code = null, Color = null }
            };
        }

        public async Task<ProjectRequestDto?> UpdateAsync(UpdateProjectRequestDto updateDto, string currentUserId)
        {
            try
            {
                var projectRequest = await _context.ProjectRequests
                    .Include(pr => pr.StatusConfig)
                    .Include(pr => pr.PriorityConfig)
                    .Include(pr => pr.RequestTypeConfig)
                    .FirstOrDefaultAsync(pr => pr.Id == updateDto.Id);

                if (projectRequest == null)
                    return null;

                var changes = new Dictionary<string, ChangeDetail>();


                // Update allowed fields
                if (!string.IsNullOrEmpty(updateDto.RequestTitle) && projectRequest.RequestTitle != updateDto.RequestTitle)
                {
                    changes.Add("RequestTitle", new ChangeDetail
                    {
                        OldValue = projectRequest.RequestTitle,
                        NewValue = updateDto.RequestTitle
                    });
                    projectRequest.RequestTitle = updateDto.RequestTitle;
                }

                if (!string.IsNullOrEmpty(updateDto.RequestDescription) && projectRequest.RequestDescription != updateDto.RequestDescription)
                {
                    changes.Add("RequestDescription", new ChangeDetail
                    {
                        OldValue = projectRequest.RequestDescription,
                        NewValue = projectRequest.RequestDescription
                    });
                   projectRequest.RequestDescription = updateDto.RequestDescription;

                }

                if (!string.IsNullOrEmpty(updateDto.ReferenceNo) && projectRequest.ReferenceNo != updateDto.ReferenceNo)
                {
                    changes.Add("RequestNumber", new ChangeDetail
                    {
                        OldValue = projectRequest.ReferenceNo,
                        NewValue = projectRequest.ReferenceNo
                    });
                    projectRequest.ReferenceNo = updateDto.ReferenceNo;
                }

                if (updateDto.StatusConfigId.HasValue && projectRequest.StatusConfigId != updateDto.StatusConfigId.Value)
                {
                    var oldStatus = projectRequest.StatusConfig?.Name ?? "Unknown";
                    var newStatus = await _configService.GetStatusByIdAsync(updateDto.StatusConfigId.Value);
                    changes.Add("Status", new ChangeDetail
                    {
                        OldValue = oldStatus,
                        NewValue = newStatus?.Name ?? "Unknown"
                    });
                    projectRequest.StatusConfigId = updateDto.StatusConfigId.Value;
                }

                if (updateDto.WorkflowStageConfigId.HasValue)
                    projectRequest.WorkflowStageConfigId = updateDto.WorkflowStageConfigId.Value;

                if (!string.IsNullOrEmpty(updateDto.AssignedTeam))
                    projectRequest.AssignedTeam = updateDto.AssignedTeam;

                if (!string.IsNullOrEmpty(updateDto.AssignedTo))
                    projectRequest.AssignedTo = updateDto.AssignedTo;

                if (!string.IsNullOrEmpty(updateDto.EvaluatorID))
                    projectRequest.EvaluatorID = updateDto.EvaluatorID;

                if (!string.IsNullOrWhiteSpace(updateDto.RequestedByName) && projectRequest.RequestedByName != updateDto.RequestedByName)
                {
                    changes.Add("RequestedByName", new ChangeDetail { OldValue = projectRequest.RequestedByName, NewValue = updateDto.RequestedByName });
                    projectRequest.RequestedByName = updateDto.RequestedByName!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.BusinessSector) && projectRequest.BusinessSector != updateDto.BusinessSector)
                {
                    changes.Add("BusinessSector", new ChangeDetail { OldValue = projectRequest.BusinessSector, NewValue = updateDto.BusinessSector });
                    projectRequest.BusinessSector = updateDto.BusinessSector!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.BusinessDivision) && projectRequest.BusinessDivision != updateDto.BusinessDivision)
                {
                    changes.Add("BusinessDivision", new ChangeDetail { OldValue = projectRequest.BusinessDivision, NewValue = updateDto.BusinessDivision });
                    projectRequest.BusinessDivision = updateDto.BusinessDivision!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.BusinessDepartment) && projectRequest.BusinessDepartment != updateDto.BusinessDepartment)
                {
                    changes.Add("BusinessDepartment", new ChangeDetail { OldValue = projectRequest.BusinessDepartment, NewValue = updateDto.BusinessDepartment });
                    projectRequest.BusinessDepartment = updateDto.BusinessDepartment!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.OrganizationName) && projectRequest.OrganizationName != updateDto.OrganizationName)
                {
                    changes.Add("OrganizationName", new ChangeDetail { OldValue = projectRequest.OrganizationName, NewValue = updateDto.OrganizationName });
                    projectRequest.OrganizationName = updateDto.OrganizationName!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.PrimaryContactEmail) && projectRequest.PrimaryContactEmail != updateDto.PrimaryContactEmail)
                {
                    changes.Add("PrimaryContactEmail", new ChangeDetail { OldValue = projectRequest.PrimaryContactEmail, NewValue = updateDto.PrimaryContactEmail });
                    projectRequest.PrimaryContactEmail = updateDto.PrimaryContactEmail!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.SecondaryContactEmail) && projectRequest.SecondaryContactEmail != updateDto.SecondaryContactEmail)
                {
                    changes.Add("SecondaryContactEmail", new ChangeDetail { OldValue = projectRequest.SecondaryContactEmail, NewValue = updateDto.SecondaryContactEmail });
                    projectRequest.SecondaryContactEmail = updateDto.SecondaryContactEmail;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.PrimaryContactPhone) && projectRequest.PrimaryContactPhone != updateDto.PrimaryContactPhone)
                {
                    changes.Add("PrimaryContactPhone", new ChangeDetail { OldValue = projectRequest.PrimaryContactPhone, NewValue = updateDto.PrimaryContactPhone });
                    projectRequest.PrimaryContactPhone = updateDto.PrimaryContactPhone!;
                }

                if (!string.IsNullOrWhiteSpace(updateDto.SecondaryContactPhone) && projectRequest.SecondaryContactPhone != updateDto.SecondaryContactPhone)
                {
                    changes.Add("SecondaryContactPhone", new ChangeDetail { OldValue = projectRequest.SecondaryContactPhone, NewValue = updateDto.SecondaryContactPhone });
                    projectRequest.SecondaryContactPhone = updateDto.SecondaryContactPhone;
                }

                if (updateDto.StrategicAlignmentConfigId.HasValue)
                {
                    var alignment = await _configService.GetStrategicAlignmentByIdAsync(updateDto.StrategicAlignmentConfigId.Value);
                    if (alignment != null && projectRequest.StrategicAlignmentConfigId != alignment.Id)
                    {
                        changes.Add("StrategicAlignment", new ChangeDetail { OldValue = projectRequest.StrategicAlignment, NewValue = alignment.Name });
                        projectRequest.StrategicAlignmentConfigId = alignment.Id;
                        projectRequest.StrategicAlignment = alignment.Name;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(updateDto.StrategicAlignment) && projectRequest.StrategicAlignment != updateDto.StrategicAlignment)
                {
                    changes.Add("StrategicAlignment", new ChangeDetail { OldValue = projectRequest.StrategicAlignment, NewValue = updateDto.StrategicAlignment });
                    projectRequest.StrategicAlignment = updateDto.StrategicAlignment!;
                    projectRequest.StrategicAlignmentConfigId = null;
                }

                if (updateDto.FeasibilityScore.HasValue)
                    projectRequest.FeasibilityScore = updateDto.FeasibilityScore.Value;

                if (updateDto.BusinessValueScore.HasValue)
                    projectRequest.BusinessValueScore = updateDto.BusinessValueScore.Value;

                if (updateDto.TechnicalComplexityScore.HasValue)
                    projectRequest.TechnicalComplexityScore = updateDto.TechnicalComplexityScore.Value;

                if (!string.IsNullOrEmpty(updateDto.EvaluationRemarks))
                    projectRequest.EvaluationRemarks = updateDto.EvaluationRemarks;

                // Recalculate total score if any evaluation scores changed
                if (updateDto.FeasibilityScore.HasValue || updateDto.BusinessValueScore.HasValue || updateDto.TechnicalComplexityScore.HasValue)
                {
                    projectRequest.TotalScore = CalculateTotalScore(
                        projectRequest.FeasibilityScore ?? 0,
                        projectRequest.BusinessValueScore ?? 0,
                        projectRequest.TechnicalComplexityScore ?? 0
                    );
                }

                // Update audit fields
                projectRequest.LastUpdatedDate = DateTime.UtcNow;
                projectRequest.LastUpdatedBy = currentUserId;

                await _context.SaveChangesAsync();

                if (changes.Any())
                {
                    await _auditService.LogUpdateAsync(projectRequest.Id, currentUserId, changes);
                }

                return await MapToProjectRequestDto(projectRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project request: {Id}", updateDto.Id);
                throw;
            }
        }



        public async Task<bool> UpdateStatusAsync(int requestId, int statusConfigId, string currentUserId)
        {
            try
            {
                var status = await _configService.GetStatusByIdAsync(statusConfigId);
                if (status == null)
                    return false;

                return await SetStatusAndStageAsync(
                    requestId,
                    status.Code,
                    null,
                    $"Status updated to {status.Name} by {currentUserId}",
                    currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for project request: {Id}", requestId);
                return false;
            }
        }

        public Task<bool> AssignEvaluatorAsync(int requestId, string evaluatorId, string currentUserId)
        {
            throw new NotImplementedException();
        }

    //    public async Task<bool> SubmitEvaluationAsync(int requestId, int feasibilityScore, int businessValueScore,
    //int technicalComplexityScore, string remarks, string currentUserId)
    //    {
    //        try
    //        {
    //            var projectRequest = await _context.ProjectRequests.FindAsync(requestId);
    //            if (projectRequest == null)
    //                return false;



    //            projectRequest.FeasibilityScore = feasibilityScore;
    //            projectRequest.BusinessValueScore = businessValueScore;
    //            projectRequest.TechnicalComplexityScore = technicalComplexityScore;
    //            projectRequest.EvaluationRemarks = remarks;
    //            projectRequest.TotalScore = CalculateTotalScore(feasibilityScore, businessValueScore, technicalComplexityScore);
    //            projectRequest.EvaluatorID = currentUserId;
    //            projectRequest.LastUpdatedDate = DateTime.UtcNow;
    //            projectRequest.LastUpdatedBy = currentUserId;

    //            await _context.SaveChangesAsync();
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error submitting evaluation for project request: {Id}", requestId);
    //            return false;
    //        }
    //    }


        public async Task<List<ProjectRequestDto>> GetByStatusAsync(int statusConfigId)
        {
            var projectRequests = await _context.ProjectRequests
                .Include(pr => pr.StatusConfig)
                .Include(pr => pr.PriorityConfig)
                .Include(pr => pr.RequestTypeConfig)
                .Where(pr => pr.StatusConfigId == statusConfigId)
                .OrderByDescending(pr => pr.CreatedDate)
                .ToListAsync();

            var dtos = new List<ProjectRequestDto>();
            foreach (var pr in projectRequests)
            {
                dtos.Add(await MapToProjectRequestDto(pr));
            }

            return dtos;
        }

        public async Task<List<ProjectRequestDto>> GetByRequesterAsync(string requesterId)
        {
            var projectRequests = await _context.ProjectRequests
                .Include(pr => pr.StatusConfig)
                .Include(pr => pr.PriorityConfig)
                .Include(pr => pr.RequestTypeConfig)
                .Where(pr => pr.RequestedBy == requesterId)
                .OrderByDescending(pr => pr.CreatedDate)
                .ToListAsync();

            var dtos = new List<ProjectRequestDto>();
            foreach (var pr in projectRequests)
            {
                dtos.Add(await MapToProjectRequestDto(pr));
            }

            return dtos;
        }

        public async Task<Dictionary<string, List<ConfigOptionDto>>> GetConfigurationOptionsAsync()
        {
            var configs = new Dictionary<string, List<ConfigOptionDto>>();

            // Get all active configurations
            var statuses = await _configService.GetActiveStatusesAsync();
            var priorities = await _configService.GetActivePrioritiesAsync();
            var requestTypes = await _configService.GetActiveRequestTypesAsync();
            var strategicAlignments = await _configService.GetActiveStrategicAlignmentsAsync();
            var requestCategories = await _context.RequestCategoryConfigs.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            var serviceCategories = await _context.ServiceCategoryConfigs.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            var productCategories = await _context.ProductCategoryConfigs.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            var impactUrgencies = await _context.ImpactUrgencyConfigs.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();

            configs["Statuses"] = statuses.Select(s => new ConfigOptionDto
            { Id = s.Id, Name = s.Name, Code = s.Code, SortOrder = s.SortOrder }).ToList();

            configs["Priorities"] = priorities.Select(p => new ConfigOptionDto
            { Id = p.Id, Name = p.Name, Code = p.Code, Color = p.Color, SortOrder = p.SortOrder }).ToList();

            configs["RequestTypes"] = requestTypes.Select(rt => new ConfigOptionDto
            { Id = rt.Id, Name = rt.Name, Code = rt.Code, SortOrder = rt.SortOrder }).ToList();

            configs["RequestCategories"] = requestCategories.Select(rc => new ConfigOptionDto
            { Id = rc.Id, Name = rc.Name, Code = rc.Code, SortOrder = rc.SortOrder }).ToList();

            configs["ServiceCategories"] = serviceCategories.Select(sc => new ConfigOptionDto
            { Id = sc.Id, Name = sc.Name, Code = sc.Code, SortOrder = sc.SortOrder }).ToList();

            configs["ProductCategories"] = productCategories.Select(pc => new ConfigOptionDto
            { Id = pc.Id, Name = pc.Name, Code = pc.Code, SortOrder = pc.SortOrder }).ToList();

            configs["ImpactLevels"] = impactUrgencies.Select(i => new ConfigOptionDto
            { Id = i.Id, Name = i.Name, Code = i.Code, Color = i.Color, SortOrder = i.SortOrder }).ToList();

            configs["UrgencyLevels"] = impactUrgencies.Select(i => new ConfigOptionDto
            { Id = i.Id, Name = i.Name, Code = i.Code, Color = i.Color, SortOrder = i.SortOrder }).ToList();

            configs["RiskLevels"] = impactUrgencies.Select(i => new ConfigOptionDto
            { Id = i.Id, Name = i.Name, Code = i.Code, Color = i.Color, SortOrder = i.SortOrder }).ToList();

            configs["ComplexityLevels"] = impactUrgencies.Select(i => new ConfigOptionDto
            { Id = i.Id, Name = i.Name, Code = i.Code, Color = i.Color, SortOrder = i.SortOrder }).ToList();

            configs["StrategicAlignments"] = strategicAlignments.Select(sa => new ConfigOptionDto
            { Id = sa.Id, Name = sa.Name, Code = sa.Code, SortOrder = sa.SortOrder }).ToList();

            return configs;
        }

        public async Task<List<AssignmentRoleDto>> GetAssignmentRolesAsync()
        {
            var roles = await _context.AssignmentRoleConfigs
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.SortOrder)
                .ToListAsync();

            return roles.Select(r => new AssignmentRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Code = r.Code,
                Priority = r.Priority,
                CanBeMultiple = r.CanBeMultiple,
                IsActive = r.IsActive,
                SortOrder = r.SortOrder,
                Description = r.Description
            }).ToList();
        }

        private decimal CalculateTotalScore(int feasibility, int businessValue, int technicalComplexity)
        {
            // Simple average - you can implement weighted scoring later
            return (feasibility + businessValue + technicalComplexity) / 3.0m;
        }

        // ASSIGN A REQUEST TO A TEAM/PERSON

        public async Task<AssignmentResult> AssignAsync(int requestId, string team, string assigneeId,
    string currentUserId, string assigneeRole = "Team Member", bool setAsPrimary = false, string reviewerType = null, bool autoCreateTasks = true)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("🚀 STARTING assignment for request {RequestId}", requestId);

                var request = await _context.ProjectRequests
                    .Include(r => r.Assignments)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                    return AssignmentResult.CreateFailure($"Request {requestId} not found");

                // Step 1: Load all active roles into memory
                var allActiveRoles = await _context.AssignmentRoleConfigs.Where(r => r.IsActive).ToListAsync();
                // Step 2: Normalize input
                string Normalize(string s) => CanonicalizeRoleName(s);
                var normalizedInputRole = Normalize(assigneeRole);
                // Step 3: Find matched config in memory (exact)
                var roleConfig = allActiveRoles.FirstOrDefault(r =>
                    Normalize(r.Code) == normalizedInputRole ||
                    Normalize(r.Name) == normalizedInputRole);

                // Step 3b: Fallback to alias mapping and contains heuristic (accept common short forms like 'innovation')
                string mappedFromAlias = null;
                if (roleConfig == null)
                {
                    var aliases = new Dictionary<string, string>
                    {
                        { "INNOVATION", "INNOVATION_CHAPTER" },
                        { "INNOVATIONCHAPTER", "INNOVATION_CHAPTER" },
                        { "SECURITY", "INFO_SECURITY" },
                        { "INFOSEC", "INFO_SECURITY" },
                        { "DEVSECOPS", "DEVSECOPS_TEAM" },
                        { "HEADENGINEERING", "HEAD_ENGINEERING" },
                        { "HEADDATAAI", "HEAD_DATA_AI" },
                        { "DATAAI", "HEAD_DATA_AI" },
                        { "PRODUCTOWNER", "PRODUCT_OWNER" },
                        { "HEADPRODUCTMGMT", "HEAD_PRODUCT_MGMT" },
                        { "HPM", "HEAD_PRODUCT_MGMT" },
                    { "BA", "BUSINESS_ANALYST" },
                    { "BUSINESSANALYST", "BUSINESS_ANALYST" },
                    { "STAKEHOLDER", "STAKEHOLDER" }
                    };
                    if (aliases.TryGetValue(normalizedInputRole, out var mappedCode))
                    {
                        mappedFromAlias = mappedCode;
                        var normMap = Normalize(mappedCode);
                        roleConfig = allActiveRoles.FirstOrDefault(r => Normalize(r.Code) == normMap || Normalize(r.Name) == normMap);
                    }
                    if (roleConfig == null)
                    {
                        var candidates = allActiveRoles.Where(r =>
                            Normalize(r.Code).Contains(normalizedInputRole) ||
                            Normalize(r.Name).Contains(normalizedInputRole)).ToList();
                        if (candidates.Count == 1)
                        {
                            roleConfig = candidates.First();
                        }
                    }
                }
                // Finalize role code/name using config if found, else derive from alias/heuristics
                string roleCode = roleConfig?.Code;
                string roleName = roleConfig?.Name;
                if (roleConfig == null)
                {
                    // Attempt to derive from alias or common patterns
                    if (!string.IsNullOrEmpty(mappedFromAlias))
                    {
                        roleCode = mappedFromAlias;
                        roleName = mappedFromAlias.Replace("_", " ").ToLowerInvariant();
                        roleName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(roleName);
                    }
                    else if (normalizedInputRole.Contains("INNOVATION"))
                    {
                        roleCode = "INNOVATION_CHAPTER";
                        roleName = "Innovation Chapter";
                    }
                    else if (normalizedInputRole.Contains("SECURITY"))
                    {
                        roleCode = "INFO_SECURITY";
                        roleName = "Information Security";
                    }
                    else if (normalizedInputRole.Contains("DEVSECOPS"))
                    {
                        roleCode = "DEVSECOPS_TEAM";
                        roleName = "DevSecOps Team";
                    }
                    else if (normalizedInputRole.Contains("HEADENGINEERING"))
                    {
                        roleCode = "HEAD_ENGINEERING";
                        roleName = "Head of Engineering";
                    }
                    else if (normalizedInputRole.Contains("PRODUCTOWNER"))
                    {
                        roleCode = "PRODUCT_OWNER";
                        roleName = "Product Owner";
                    }
                    else if (normalizedInputRole.Contains("HEADDATAAI") || normalizedInputRole.Contains("DATAAI"))
                    {
                        roleCode = "HEAD_DATA_AI";
                        roleName = "Head of Data & AI";
                    }
                    else if (normalizedInputRole == "BA")
                    {
                        roleCode = "BUSINESS_ANALYST";
                        roleName = "Business Analyst";
                    }
                    else
                    {
                        return AssignmentResult.CreateFailure($"Role '{assigneeRole}' is not configured or inactive");
                    }
                }

                // continue using roleConfig.Code/Name below this for downstream logic
                var canonicalRoleName = CanonicalizeRoleName(roleName);
                if (!roleConfig.CanBeMultiple)
                {
                    var closed = await CloseExistingAssignmentsForRoleAsync(requestId, canonicalRoleName, roleName ?? roleCode ?? assigneeRole, currentUserId);
                    if (closed > 0)
                    {
                        _logger.LogInformation("♻️ Replaced {Count} existing {RoleName} assignment(s) before creating a new one", closed, roleName);
                    }
                }

                // Step 4: Determine if this role is an evaluator role and set ReviewerType automatically
                var evaluatorRoleCodes = new[] { "HEAD_PRODUCT_MGMT", "BUSINESS_ANALYST", "INNOVATION_CHAPTER", "SUBJECT_MATTER_EXPERT" };
                var reviewerRoleCodes = new[] { "HEAD_ENGINEERING", "HEAD_DATA_AI", "PRODUCT_OWNER", "INFO_SECURITY", "DEVSECOPS_TEAM" };
                var approverRoleCodes = new[] { "VP_DIRECTOR", "COMPLIANCE_OFFICER", "PMO" };

                bool isEvaluatorRole = evaluatorRoleCodes.Contains(roleCode.ToUpperInvariant());
                bool isReviewerRole = reviewerRoleCodes.Contains(roleCode.ToUpperInvariant());
                bool isApproverRole = approverRoleCodes.Contains(roleCode.ToUpperInvariant());

                // Auto-determine ReviewerType if not provided
                if (string.IsNullOrEmpty(reviewerType))
                {
                    if (isEvaluatorRole)
                        reviewerType = "EVALUATOR";
                    else if (isReviewerRole)
                        reviewerType = "REVIEWER";
                    else if (isApproverRole)
                        reviewerType = "APPROVER";
                }

                _logger.LogInformation("Role {Role} classified as: Evaluator={IsEval}, Reviewer={IsRev}, Approver={IsApp}, ReviewerType={ReviewerType}",
                    roleCode, isEvaluatorRole, isReviewerRole, isApproverRole, reviewerType);

                // Validate assignment
                var validationErrors = await _assignmentStrategy.ValidateAssignmentAsync(requestId, roleName, assigneeId);

                if (validationErrors.Any())
                    return AssignmentResult.CreateFailure(validationErrors);

                // STEP 1: Create and SAVE assignment first
                var assignment = new ProjectRequestAssignment
                {
                    ProjectRequestId = requestId,
                    AssigneeId = assigneeId,
                    // Store canonical role name to align with dynamic role configuration
                    AssigneeRole = roleName,

                    ReviewerType = reviewerType,
                    AssignmentNotes = $"Assigned to {team} as {assigneeRole}",
                    AssignedDate = DateTime.UtcNow,
                    IsPrimary = setAsPrimary,
                    IsPrimaryEvaluator = false
                };

                _context.ProjectRequestAssignments.Add(assignment);
                await _context.SaveChangesAsync(); // SAVE to get ID
                _logger.LogInformation("✅ Assignment created with ID: {AssignmentId}", assignment.Id);

                // STEP 2: Create evaluation (if evaluator role - using dynamic config)
                if (isEvaluatorRole)
                {
                    try
                    {
                        await CreateEvaluationFromAssignmentAsync(requestId, assigneeId, assignment.Id, setAsPrimary);
                        await _context.SaveChangesAsync(); // SAVE evaluation
                        _logger.LogInformation("✅ Evaluation created for assignment {AssignmentId} with role {Role}", assignment.Id, assigneeRole);
                    }
                    catch (Exception evalEx)
                    {
                        _logger.LogError(evalEx, "❌ Evaluation creation failed, but continuing assignment");
                        // Don't fail the entire assignment if evaluation fails
                    }
                }

                if (!string.IsNullOrEmpty(reviewerType))
                {
                    assignment.ReviewerType = reviewerType;
                    _logger.LogInformation("📝 Stored ReviewerType '{ReviewerType}' for manual task creation later", reviewerType);
                }

                // STEP 3: Handle primary assignments
                if (setAsPrimary)
                {
                    try
                    {
                        await _assignmentStrategy.SetPrimaryAssigneeAsync(requestId, assigneeId, currentUserId);
                        if (isEvaluatorRole)
                        {
                            await _assignmentStrategy.SetPrimaryEvaluatorAsync(requestId, assigneeId, currentUserId);
                        }
                        _logger.LogInformation("✅ Primary assignment handled");
                    }
                    catch (Exception primaryEx)
                    {
                        _logger.LogError(primaryEx, "❌ Primary assignment failed, but continuing");
                    }
                }

                // STEP 4: Handle workflow stage
                try
                {
                    await HandleWorkflowStageTransitionAsync(requestId, currentUserId);
                    _logger.LogInformation("✅ Workflow stage handled");
                }
                catch (Exception workflowEx)
                {
                    _logger.LogError(workflowEx, "❌ Workflow stage failed, but continuing");
                }

                // STEP 5: Update main request fields (CRITICAL - this might be failing)
                try
                {
                    request.AssignedTeam = team;
                    // ✅ FIXED: AssignedTo should only hold creator/primary owner, NOT reviewers
                    // Clarification: 
                    // - "Head of Product Management" is a role (HEAD_PRODUCT_MGMT) that can be assigned to reviewers
                    // - "Head Reviewer" is the workflow owner for THIS request (assigned via assign-head endpoint)
                    // - AssignedTo should only be updated if this is the Head Reviewer (workflow owner), not just any Head role
                    // - For now, we keep AssignedTo as the creator unless explicitly set via assign-head endpoint
                    // All reviewers (including Head of Product Management) are tracked in ProjectRequestAssignments table
                    
                    request.EvaluatorID = await _assignmentStrategy.GetPrimaryEvaluatorAsync(requestId);
                    request.LastUpdatedDate = DateTime.UtcNow;
                    request.LastUpdatedBy = currentUserId;

                    await _context.SaveChangesAsync(); // SAVE request updates
                    _logger.LogInformation("✅ Main request fields updated: AssignedTo={AssignedTo}, EvaluatorID={EvaluatorID}",
                        request.AssignedTo, request.EvaluatorID);

                    if (!string.IsNullOrEmpty(request.AssignedTo))
                    {
                        var assigneeInfo = await _userService.GetUserByEmployeeIdAsync(request.AssignedTo);
                        var assigneeName = assigneeInfo?.FullName ?? request.AssignedTo;
                        await _ownerHistoryService.TrackOwnerChangeAsync(requestId, request.AssignedTo, assigneeName, roleName, currentUserId);
                    }
                }
                catch (Exception requestEx)
                {
                    _logger.LogError(requestEx, "❌ Request update failed - this is critical!");
                    throw; // Re-throw this one since it's critical
                }

                // ✅ STEP 6: Conditionally create review tasks (if autoCreateTasks is true)
                if (autoCreateTasks)
                {
                    try
                    {
                        // Only create tasks for evaluator/reviewer roles, not approvers
                        // Note: evaluatorRoleCodes and reviewerRoleCodes are already declared above (lines 1001-1002)
                        var normalizedRoleCode = CanonicalizeRoleName(roleCode ?? assigneeRole);
                        bool shouldCreateTasks = evaluatorRoleCodes.Any(c => CanonicalizeRoleName(c) == normalizedRoleCode) ||
                                               reviewerRoleCodes.Any(c => CanonicalizeRoleName(c) == normalizedRoleCode);

                        if (shouldCreateTasks && !string.IsNullOrEmpty(reviewerType))
                        {
                            await _reviewTaskService.CreateDynamicReviewTasksAsync(requestId, assigneeId, reviewerType, currentUserId);
                            _logger.LogInformation("✅ Auto-created review tasks for {Role} on request {RequestId}", assigneeRole, requestId);
                        }
                    }
                    catch (Exception taskEx)
                    {
                        _logger.LogError(taskEx, "❌ Failed to auto-create review tasks, but assignment succeeded");
                        // Don't fail assignment if task creation fails
                    }
                }
                else
                {
                    _logger.LogInformation("⏭️ Skipping auto-creation of review tasks (autoCreateTasks=false)");
                }

                // ✅ STEP 7: Commit transaction
                await transaction.CommitAsync();

                var underEvaluationStatus = await _configService.GetStatusByCodeAsync("UNDER_EVALUATION");
                if (underEvaluationStatus != null && request.StatusConfigId != underEvaluationStatus.Id)
                {
                    await SetStatusAndStageAsync(
                        requestId,
                        "UNDER_EVALUATION",
                        "INITIAL_EVAL",
                        $"Auto-transition after assignment of {assigneeRole}",
                        currentUserId);
                }

                _logger.LogInformation("🎉 SUCCESS: Assignment completed for request {RequestId}", requestId);
                return AssignmentResult.CreateSuccess(request.AssignedTo);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ COMPLETE FAILURE: Assignment rolled back for request {RequestId}", requestId);
                return AssignmentResult.CreateFailure($"Assignment failed: {ex.Message}. Inner: {ex.InnerException?.Message}");
            }
        }

        private async Task UpdatePrimaryEvaluationStatusAsync(int requestId, string evaluatorId)
        {
            try
            {
                // Find the evaluation record for this assignee
                var evaluation = await _context.ProjectRequestEvaluations
                    .FirstOrDefaultAsync(e => e.ProjectRequestId == requestId && e.ReviewerId == evaluatorId);

                if (evaluation != null)
                {
                    // Clear existing primary evaluations
                    var existingPrimaryEvaluations = await _context.ProjectRequestEvaluations
                        .Where(e => e.ProjectRequestId == requestId && e.IsPrimaryEvaluation)
                        .ToListAsync();

                    foreach (var existing in existingPrimaryEvaluations)
                    {
                        existing.IsPrimaryEvaluation = false;
                    }

                    // Set new primary evaluation
                    evaluation.IsPrimaryEvaluation = true;

                    _logger.LogInformation("✅ Updated primary evaluation for request {RequestId} to evaluator {EvaluatorId}",
                        requestId, evaluatorId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating primary evaluation status for request {RequestId}", requestId);
            }
        }

        private static string CanonicalizeRoleName(string? roleValue)
        {
            return (roleValue ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToUpperInvariant();
        }

        private async Task<int> CloseExistingAssignmentsForRoleAsync(int requestId, string canonicalRoleName, string? roleDisplayName, string currentUserId)
        {
            var activeAssignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId && !a.CompletedDate.HasValue)
                .ToListAsync();

            var toClose = activeAssignments
                .Where(a => CanonicalizeRoleName(a.AssigneeRole) == canonicalRoleName)
                .ToList();

            if (!toClose.Any())
                return 0;

            var assignmentIds = toClose.Select(a => a.Id).ToList();
            var linkedEvaluations = await _context.ProjectRequestEvaluations
                .Where(e => e.ProjectRequestAssignmentId.HasValue && assignmentIds.Contains(e.ProjectRequestAssignmentId.Value))
                .ToListAsync();

            if (linkedEvaluations.Any())
            {
                _context.ProjectRequestEvaluations.RemoveRange(linkedEvaluations);
            }

            foreach (var assignment in toClose)
            {
                assignment.CompletedDate = DateTime.UtcNow;
                assignment.AssignmentNotes = (assignment.AssignmentNotes ?? string.Empty) +
                    $"\nAuto-completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} when {roleDisplayName ?? "the role"} was reassigned by {currentUserId}.";
            }

            await _context.SaveChangesAsync();
            return toClose.Count;
        }

        private async Task HandleWorkflowStageTransitionAsync(int requestId, string currentUserId)
        {
            try
            {
                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null) return;

                // If this is the first assignment, move to "Initial Evaluation" stage
                if (!request.WorkflowStageConfigId.HasValue)
                {
                    // ✅ FIXED: Use dynamic config with IsActive check
                    var initialEvaluationStage = await _context.WorkflowStageConfigs
                        .FirstOrDefaultAsync(w => w.Code == "INITIAL_EVAL" && w.IsActive);

                    if (initialEvaluationStage != null)
                    {
                        // Update the request's workflow stage
                        request.WorkflowStageConfigId = initialEvaluationStage.Id;

                        // ✅ CREATE WORKFLOW HISTORY RECORD
                        await _workflowService.UpdateWorkflowStageAsync(requestId, initialEvaluationStage.Id, currentUserId);

                        _logger.LogInformation("✅ Set workflow stage to 'Initial Evaluation' for request {RequestId}", requestId);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Initial Evaluation workflow stage not found in configuration");
                    }

                    // Also update status to "Under Evaluation"
                    var underEvaluationStatus = await _configService.GetStatusByNameAsync("Under Evaluation");
                    if (underEvaluationStatus != null && request.StatusConfigId != underEvaluationStatus.Id)
                    {
                        await _workflowService.TransitionToStatusAsync(requestId, underEvaluationStatus.Id, currentUserId,
                            "First assignment - moving to Under Evaluation status");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error handling workflow stage transition for request {RequestId}", requestId);
                // Don't throw - workflow transition shouldn't break assignment
            }
        }

        private async Task CreateEvaluationFromAssignmentAsync(int requestId, string assigneeId, int assignmentId, bool isPrimary)
        {
            try
            {
                // ✅ FIX 1: Get the actual assignment to determine role and primary status
                var assignment = await _context.ProjectRequestAssignments
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("❌ Assignment {AssignmentId} not found for evaluation creation", assignmentId);
                    return;
                }

                // ✅ FIX 2: Use the parameter OR assignment flag (whichever is more recent)
                var shouldBePrimary = isPrimary || assignment.IsPrimaryEvaluator;

                // ✅ FIX 3: Map assignment role to reviewer role dynamically
                var reviewerRole = MapAssigneeRoleToReviewerRole(assignment.AssigneeRole);

                // ✅ FIX 4: Properly initialize draft evaluation with NULL scores
                var evaluation = new ProjectRequestEvaluation
                {
                    ProjectRequestId = requestId,
                    ProjectRequestAssignmentId = assignmentId,
                    ReviewerId = assigneeId,
                    ReviewerName = assigneeId, // Would come from AD in real scenario
                    ReviewerRole = reviewerRole, // ✅ Dynamic role mapping
                    StrategicAlignmentScore = null, // ✅ NULL for drafts
                    FeasibilityScore = null,        // ✅ NULL for drafts  
                    BusinessValueScore = null,      // ✅ NULL for drafts
                    TechnicalComplexityScore = null, // ✅ NULL for drafts
                    EvaluationRemarks = null,        // ✅ Explicitly null
                    EvaluationStatus = EvaluationStatus.Draft, // ✅ Proper enum value
                    IsPrimaryEvaluation = shouldBePrimary, // ✅ Combined logic
                    CreatedAt = DateTime.UtcNow,
                    SubmittedAt = null // ✅ NULL until actually submitted
                };

                _context.ProjectRequestEvaluations.Add(evaluation);

                _logger.LogInformation("📝 Created evaluation record for assignee {Assignee} as {Role} (Primary: {IsPrimary}) on request {RequestId}",
                    assigneeId, reviewerRole, shouldBePrimary, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating evaluation record for assignment {AssignmentId}", assignmentId);
                // ✅ DON'T throw - evaluation creation failure shouldn't break assignment
            }
        }

        // ✅ ADD THIS MAPPING METHOD
        // ✅ FIXED: Map to ReviewerRole enum using dynamic config (supports both config names and requirement names)
        private ReviewerRole MapAssigneeRoleToReviewerRole(string assigneeRole)
        {
            if (string.IsNullOrEmpty(assigneeRole))
                return ReviewerRole.BusinessAnalyst;

            // ✅ Use dynamic config first, then fallback to requirement names
            // This supports both admin-configured roles and requirement-specific roles
            var normalizedRole = assigneeRole.ToLowerInvariant();
            
            // Map from config codes (dynamic)
            if (normalizedRole.Contains("head_product") || normalizedRole.Contains("head of product management"))
                return ReviewerRole.HeadOfProductManagement;
            if (normalizedRole.Contains("business_analyst") || normalizedRole.Contains("business analyst"))
                return ReviewerRole.BusinessAnalyst;
            if (normalizedRole.Contains("innovation_chapter") || normalizedRole.Contains("innovation chapter"))
                return ReviewerRole.InnovationChapter;
            if (normalizedRole.Contains("subject_matter") || normalizedRole.Contains("subject matter expert") || normalizedRole.Contains("sme"))
                return ReviewerRole.SubjectMatterExpert;
            if (normalizedRole.Contains("head_engineering") || normalizedRole.Contains("head of engineering"))
                return ReviewerRole.HeadOfEngineering;
            if (normalizedRole.Contains("info_security") || normalizedRole.Contains("information security") || normalizedRole.Contains("security"))
                return ReviewerRole.InformationSecurity;
            
            // Default fallback
            return ReviewerRole.BusinessAnalyst;
        }
        public async Task<bool> SetPrimaryAssigneeAsync(int requestId, string assigneeId, string currentUserId)
        {
            try
            {

                var result = await _assignmentStrategy.SetPrimaryAssigneeAsync(requestId, assigneeId, currentUserId);


                if (result)
                {
                    // Update the main request's assigned fields
                    var request = await _context.ProjectRequests.FindAsync(requestId);
                    if (request != null)
                    {
                        request.AssignedTo = await _assignmentStrategy.GetPrimaryAssigneeAsync(requestId);
                        request.EvaluatorID = await _assignmentStrategy.GetPrimaryEvaluatorAsync(requestId);
                        request.LastUpdatedDate = DateTime.UtcNow;
                        request.LastUpdatedBy = currentUserId;
                        await _context.SaveChangesAsync();

                        if (!string.IsNullOrEmpty(request.AssignedTo))
                        {
                            var assigneeInfo = await _userService.GetUserByEmployeeIdAsync(request.AssignedTo);
                            var assigneeName = assigneeInfo?.FullName ?? request.AssignedTo;
                            await _ownerHistoryService.TrackOwnerChangeAsync(requestId, request.AssignedTo, assigneeName, "PrimaryAssignee", currentUserId);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary assignee for request {RequestId}", requestId);
                return false;
            }
        }



        public async Task<PrimaryAssigneeInfo> GetPrimaryAssigneeInfoAsync(int requestId)
{
    try
    {
        var assignments = await _context.ProjectRequestAssignments
            .Where(a => a.ProjectRequestId == requestId)
            .OrderBy(a => a.AssignedDate)
            .ToListAsync();

        var primaryAssigneeId = await _assignmentStrategy.GetPrimaryAssigneeAsync(requestId);
        var primaryAssignment = assignments.FirstOrDefault(a => a.AssigneeId == primaryAssigneeId);
        
        // ✅ FIXED: Get ACTIVE evaluators only
        var primaryEvaluatorId = await _assignmentStrategy.GetPrimaryEvaluatorAsync(requestId);
        var allEvaluators = await _assignmentStrategy.GetEvaluatorsAsync(requestId);
        
        var activeAssignmentsCount = assignments.Count(a => !a.CompletedDate.HasValue);
        var roleConfigurations = await _assignmentStrategy.GetRolePriorityAsync();

        return new PrimaryAssigneeInfo
        {
            PrimaryAssigneeId = primaryAssigneeId,
            PrimaryAssigneeRole = primaryAssignment?.AssigneeRole,
            EvaluatorId = primaryEvaluatorId, // ✅ Now should not be null if there's an active primary evaluator
            EvaluatorIds = allEvaluators, // ✅ Now should show active evaluators
            ActiveAssignmentsCount = activeAssignmentsCount,
            AllAssignments = assignments,
            RoleConfigurations = roleConfigurations
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error getting primary assignee info for request {RequestId}", requestId);
        return new PrimaryAssigneeInfo();
    }
}

        public async Task<bool> CompleteAssignmentAsync(int assignmentId, string currentUserId)
        {
            try
            {
                _logger.LogInformation("✅ Completing assignment {AssignmentId} by user {UserId}", assignmentId, currentUserId);

                var assignment = await _context.ProjectRequestAssignments
                    .Include(a => a.ProjectRequest)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("❌ Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                if (assignment.AssigneeId != currentUserId)
                {
                    _logger.LogWarning("🚫 SECURITY VIOLATION: User {UserId} attempted to complete assignment {AssignmentId} owned by {AssigneeId}",
                        currentUserId, assignmentId, assignment.AssigneeId);
                    return false;
                }

                if (assignment.CompletedDate.HasValue)
                {
                    _logger.LogWarning("⚠️ Assignment {AssignmentId} is already completed", assignmentId);
                    return false;
                }

                assignment.CompletedDate = DateTime.UtcNow;
                assignment.AssignmentNotes += $"\nCompleted by {currentUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

                // ✅ FIXED: Use the updated method
                await UpdatePrimaryAssigneeAsync(assignment.ProjectRequestId);

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully completed assignment {AssignmentId}", assignmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error completing assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<bool> RevokeAssignmentAsync(int assignmentId, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Revoking assignment {AssignmentId} by user {UserId}", assignmentId, currentUserId);

                var assignment = await _context.ProjectRequestAssignments
                    .Include(a => a.ProjectRequest)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                if (assignment.CompletedDate.HasValue)
                {
                    _logger.LogWarning("Assignment {AssignmentId} is already completed or revoked", assignmentId);
                    return false;
                }

                assignment.CompletedDate = DateTime.UtcNow;
                assignment.AssignmentNotes += $"\nRevoked by {currentUserId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

                var linkedEvaluations = await _context.ProjectRequestEvaluations
                    .Where(e => e.ProjectRequestAssignmentId == assignmentId)
                    .ToListAsync();

                if (linkedEvaluations.Any())
                {
                    _context.ProjectRequestEvaluations.RemoveRange(linkedEvaluations);
                }

                await _context.SaveChangesAsync();

                await UpdatePrimaryAssigneeAsync(assignment.ProjectRequestId);

                _logger.LogInformation("Successfully revoked assignment {AssignmentId}", assignmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        // ✅ Make sure this method exists in your service:
        private async Task UpdatePrimaryAssigneeAsync(int requestId)
        {
            try
            {
                var request = await _context.ProjectRequests
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request != null)
                {
                    // ✅ FIXED: Use the new method that takes requestId, not assignments list
                    var newPrimaryAssignee = await _assignmentStrategy.GetPrimaryAssigneeAsync(requestId);

                    // Only update if changed
                    if (request.AssignedTo != newPrimaryAssignee)
                    {
                        request.AssignedTo = newPrimaryAssignee;
                        _logger.LogInformation("🔄 Updated primary assignee for request {RequestId} to {Assignee}",
                            requestId, newPrimaryAssignee);
                    }

                    // ✅ FIXED: Use the new method that gets the PRIMARY evaluator (not first from list)
                    var primaryEvaluator = await _assignmentStrategy.GetPrimaryEvaluatorAsync(requestId);
                    request.EvaluatorID = primaryEvaluator;

                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(request.AssignedTo))
                    {
                        var assigneeInfo = await _userService.GetUserByEmployeeIdAsync(request.AssignedTo);
                        var assigneeName = assigneeInfo?.FullName ?? request.AssignedTo;
                        var changedBy = request.LastUpdatedBy ?? "system";
                        await _ownerHistoryService.TrackOwnerChangeAsync(requestId, request.AssignedTo, assigneeName, "PrimaryAssignee", changedBy);
                    }

                    _logger.LogInformation("✅ Updated primary assignee and evaluator for request {RequestId}", requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating primary assignee for request {RequestId}", requestId);
                // Don't throw - this is a background update
            }
        }

        // SUBMIT EVALUATION SCORES AND REMARKS
        public async Task<ProjectRequestDto> SubmitEvaluationAsync(int requestId, SubmitEvaluationDto evaluationDto, string currentUserId)
        {
            try
            {
                _logger.LogInformation("📊 Submitting evaluation for request {RequestId}", requestId);

                var request = await GetProjectRequestWithIncludes()
                    .FirstOrDefaultAsync(pr => pr.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("❌ Request {RequestId} not found for evaluation", requestId);
                    throw new ArgumentException("Request not found");
                }

                if (request.AssignedTo != currentUserId && !await IsUserAssignedAsEvaluatorAsync(requestId, currentUserId))
                {
                    throw new UnauthorizedAccessException("You are not authorized to evaluate this request");
                }

                var underEvaluationStatus = await _configService.GetStatusByNameAsync("Under Evaluation");
                if (request.StatusConfigId != underEvaluationStatus?.Id)
                {
                    throw new InvalidOperationException("Cannot evaluate request that is not in 'Under Evaluation' status");
                }

                // Update evaluation fields
                request.FeasibilityScore = evaluationDto.FeasibilityScore;
                request.BusinessValueScore = evaluationDto.BusinessValueScore;
                request.TechnicalComplexityScore = evaluationDto.TechnicalComplexityScore;
                request.EvaluationRemarks = evaluationDto.EvaluationRemarks;
                request.EvaluatorID = currentUserId; // Use provided or current user
                request.LastUpdatedDate = DateTime.UtcNow;
                request.LastUpdatedBy = currentUserId;

                // Calculate and set total score (average of three scores)
                request.TotalScore = (evaluationDto.FeasibilityScore +
                                     evaluationDto.BusinessValueScore +
                                     evaluationDto.TechnicalComplexityScore) / 3.0m;

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

                var evaluationAssignments = await _context.ProjectRequestAssignments
                    .Where(a => a.ProjectRequestId == requestId && a.AssigneeId == currentUserId && !a.CompletedDate.HasValue)
                    .ToListAsync();

                var evaluationAssignment = evaluationAssignments.FirstOrDefault(a =>
                    evaluatorRoleNames.Any(name =>
                        (name ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant() ==
                        (a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()
                    ));

                if (evaluationAssignment != null)
                {
                    evaluationAssignment.CompletedDate = DateTime.UtcNow;
                }



                _logger.LogInformation("✅ Calculated total score: {TotalScore} for request {RequestId}",
                    request.TotalScore, requestId);

                await _context.SaveChangesAsync();

                // Return updated request
                return await MapToProjectRequestDto(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error submitting evaluation for request {RequestId}", requestId);
                throw;
            }
        }

        private async Task<bool> IsUserAssignedAsEvaluatorAsync(int requestId, string userId)
        {
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

            var assignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId && a.AssigneeId == userId && !a.CompletedDate.HasValue)
                .ToListAsync();

            return assignments.Any(a => evaluatorRoleNames.Any(name =>
                (name ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant() ==
                (a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()
            ));
        }


        // APPROVE A REQUEST
        public async Task<bool> ApproveAsync(int requestId, string remarks, string currentUserId, decimal? minimumScoreThreshold = 6.0m)
        {
            try
            {
                // ✅ FIXED: Attachments are now optional - no mandatory check
                // Only log attachment status for information, don't block approval
                try
                {
                    var attachmentCompliance = await _attachmentComplianceService.ValidateAsync(requestId);
                    if (!attachmentCompliance.IsCompliant)
                    {
                        _logger.LogInformation("Request {RequestId} has optional missing attachments: {MissingCategories} (not blocking approval)",
                            requestId, string.Join(", ", attachmentCompliance.MissingCategories));
                    }
                }
                catch (Exception attachEx)
                {
                    _logger.LogWarning(attachEx, "Could not check attachment compliance for request {RequestId} - continuing with approval", requestId);
                    // Don't fail approval if attachment check fails
                }

                // ✅ VALIDATION CHECKS (scores, evaluators, status)
                if (!await CanBeApprovedAsync(requestId, minimumScoreThreshold))
                {
                    _logger.LogWarning("Request {RequestId} cannot be approved - validation failed", requestId);
                    return false;
                }

                var approvedStatus = await _configService.GetStatusByNameAsync("Approved");
                if (approvedStatus == null) return false;

                // Use workflow service for status transition
                var result = await _workflowService.TransitionToStatusAsync(
                    requestId, approvedStatus.Id, currentUserId,
                    $"Approved by {currentUserId}. Remarks: {remarks}");

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId}", requestId);
                return false;
            }
        }


        public async Task<bool> CanBeApprovedAsync(int requestId, decimal? minimumScoreThreshold = 6.0m)
        {
            try
            {
                var request = await _context.ProjectRequests
                    .Include(r => r.Assignments)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request {RequestId} not found for approval check", requestId);
                    return false;
                }

                // ✅ FIX 1: Must have evaluation scores
                if (!request.TotalScore.HasValue)
                {
                    _logger.LogInformation("Cannot approve request {RequestId}: No evaluation scores", requestId);
                    return false;
                }
                
                // ✅ FIXED: Attachments are completely optional - no check needed

                // ✅ FIX 2: Must meet minimum score threshold
                var threshold = minimumScoreThreshold ?? 6.0m;
                if (request.TotalScore < threshold)
                {
                    _logger.LogInformation("Cannot approve request {RequestId}: Score {Score} below threshold {Threshold}",
                        requestId, request.TotalScore, threshold);
                    return false;
                }

                // ✅ FIX 3: Only check assigned evaluators - they must have submitted evaluations
                // Get all evaluator assignments for this request (not all possible evaluator roles)
                var evaluatorRoleCodes = new[] { "HEAD_PRODUCT_MGMT", "BUSINESS_ANALYST", "INNOVATION_CHAPTER", "SUBJECT_MATTER_EXPERT" };
                
                var assignedEvaluators = await _context.ProjectRequestAssignments
                    .Where(a => a.ProjectRequestId == requestId && !a.CompletedDate.HasValue)
                    .ToListAsync();

                // Normalize role matching
                var normalizedEvaluatorCodes = evaluatorRoleCodes.Select(c => 
                    c.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant()).ToList();

                var assignedEvaluatorAssignments = assignedEvaluators.Where(a =>
                {
                    var normalizedRole = (a.AssigneeRole ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
                    return normalizedEvaluatorCodes.Any(code => normalizedRole.Contains(code) || code.Contains(normalizedRole));
                }).ToList();

                // Check if assigned evaluators have submitted evaluations
                if (assignedEvaluatorAssignments.Any())
                {
                    var evaluatorIds = assignedEvaluatorAssignments.Select(a => a.AssigneeId).ToList();
                    var submittedEvaluations = await _context.ProjectRequestEvaluations
                        .Where(e => e.ProjectRequestId == requestId && 
                                    evaluatorIds.Contains(e.ReviewerId) &&
                                    e.EvaluationStatus == EvaluationStatus.Submitted)
                        .Select(e => e.ReviewerId)
                        .ToListAsync();

                    var pendingEvaluatorIds = evaluatorIds.Except(submittedEvaluations).ToList();
                    if (pendingEvaluatorIds.Any())
                    {
                        var pendingNames = assignedEvaluatorAssignments
                            .Where(a => pendingEvaluatorIds.Contains(a.AssigneeId))
                            .Select(a => a.AssigneeId)
                            .ToList();
                        _logger.LogInformation("❌ Cannot approve request {RequestId}: {Count} assigned evaluators haven't submitted evaluations: {Evaluators}",
                            requestId, pendingNames.Count, string.Join(", ", pendingNames));
                        return false;
                    }
                }

                // ✅ FIX 4: Must be in correct status (Under Evaluation)
                var underEvaluationStatus = await _configService.GetStatusByNameAsync("Under Evaluation");
                if (underEvaluationStatus == null || request.StatusConfigId != underEvaluationStatus.Id)
                {
                    _logger.LogInformation("Cannot approve request {RequestId}: Not in Under Evaluation status", requestId);
                    return false;
                }

                _logger.LogInformation("✅ Request {RequestId} can be approved - all criteria met", requestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking approval eligibility for request {RequestId}", requestId);
                return false;
            }
        }


        public async Task<ApprovalDashboardDto> GetApprovalDashboardAsync()
        {
            try
            {
                var approvalReadyStage = await _context.WorkflowStageConfigs
                    .FirstOrDefaultAsync(w => w.Code == "APPROVAL_READY");

                if (approvalReadyStage == null)
                {
                    _logger.LogWarning("Approval Ready stage not found");
                    return new ApprovalDashboardDto();
                }

                // Get requests ready for approval
                var approvalReadyRequests = await _context.ProjectRequests
                    .Include(pr => pr.StatusConfig)
                    .Include(pr => pr.PriorityConfig)
                    .Include(pr => pr.RequestTypeConfig)
                    .Where(pr => pr.WorkflowStageConfigId == approvalReadyStage.Id)
                    .OrderByDescending(pr => pr.TotalScore)
                    .ThenByDescending(pr => pr.CreatedDate)
                    .ToListAsync();

                var dashboard = new ApprovalDashboardDto
                {
                    TotalReadyForApproval = approvalReadyRequests.Count,
                    HighPriorityCount = approvalReadyRequests.Count(r => r.PriorityConfig?.Code == "P1"),
                    AverageScore = approvalReadyRequests.Any() ?
                        approvalReadyRequests.Average(r => r.TotalScore ?? 0) : 0,
                    ScoreDistribution = approvalReadyRequests
                        .GroupBy(r => Math.Floor(r.TotalScore ?? 0))
                        .ToDictionary(g => $"{g.Key}-{g.Key + 1}", g => g.Count()),
                    Requests = approvalReadyRequests.Select(r => new ApprovalReadyRequestDto
                    {
                        Id = r.Id,
                        RequestID = r.RequestID,
                        RequestTitle = r.RequestTitle,
                        TotalScore = r.TotalScore,
                        Priority = r.PriorityConfig?.Name,
                        PriorityColor = r.PriorityConfig?.Color,
                        RequestType = r.RequestTypeConfig?.Name,
                        CreatedDate = r.CreatedDate,
                        DaysInReview = (int)(DateTime.UtcNow - r.CreatedDate).TotalDays,
                        CanAutoApprove = r.TotalScore >= 7.5m
                    }).ToList()
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval dashboard");
                return new ApprovalDashboardDto();
            }
        }

        public async Task<List<ProjectRequestDto>> GetRequestsReadyForApprovalAsync()
        {
            try
            {
                var approvalReadyStage = await _context.WorkflowStageConfigs
                    .FirstOrDefaultAsync(w => w.Code == "APPROVAL_READY");

                if (approvalReadyStage == null)
                    return new List<ProjectRequestDto>();

                var requests = await _context.ProjectRequests
                    .Include(pr => pr.StatusConfig)
                    .Include(pr => pr.PriorityConfig)
                    .Include(pr => pr.RequestTypeConfig)
                    .Where(pr => pr.WorkflowStageConfigId == approvalReadyStage.Id)
                    .OrderByDescending(pr => pr.TotalScore)
                    .ThenBy(pr => pr.CreatedDate)
                    .ToListAsync();

                var dtos = new List<ProjectRequestDto>();
                foreach (var request in requests)
                {
                    dtos.Add(await MapToProjectRequestDto(request));
                }

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests ready for approval");
                return new List<ProjectRequestDto>();
            }
        }
        // REJECT A REQUEST
        public async Task<bool> RejectAsync(int requestId, string remarks, RejectionReason reason, string currentUserId)
        {
            try
            {
                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null) return false;

                var rejectedStatus = await _configService.GetStatusByNameAsync("Rejected");
                if (rejectedStatus == null) return false;

                // ✅ SET REJECTION TRACKING FIELDS
                request.RejectionReason = reason;
                request.RejectionRemarks = remarks;
                request.RejectedBy = currentUserId;
                request.RejectionDate = DateTime.UtcNow;

                // Use workflow service for status transition
                var result = await _workflowService.TransitionToStatusAsync(
                    requestId, rejectedStatus.Id, currentUserId,
                    $"Rejected by {currentUserId}. Reason: {reason}. Remarks: {remarks}");

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}", requestId);
                return false;
            }
        }

        public async Task<bool> BacklogAsync(int requestId, string reason, string currentUserId)
        {
            try
            {
                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null) return false;

                request.RejectionRemarks = reason;
                await _context.SaveChangesAsync();

                return await SetStatusAndStageAsync(
                    requestId,
                    "BACKLOGGED",
                    "SUBMITTED",
                    $"Request backlogged: {reason}",
                    currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backlogging request {RequestId}", requestId);
                return false;
            }
        }

        public async Task<bool> ReactivateFromBacklogAsync(int requestId, string currentUserId)
        {
            return await SetStatusAndStageAsync(
                requestId,
                "UNDER_EVALUATION",
                "INITIAL_EVAL",
                "Request reactivated from backlog",
                currentUserId);
        }

        public async Task<bool> StartExecutionAsync(int requestId, string currentUserId)
        {
            return await SetStatusAndStageAsync(
                requestId,
                "IN_PROGRESS",
                "IN_DEVELOPMENT",
                "Execution started",
                currentUserId);
        }

        public async Task<bool> MarkCompletedAsync(int requestId, string currentUserId)
        {
            return await SetStatusAndStageAsync(
                requestId,
                "COMPLETED",
                "COMPLETED",
                "Request development completed",
                currentUserId);
        }

        public async Task<bool> MarkDeliveredAsync(int requestId, string currentUserId)
        {
            return await SetStatusAndStageAsync(
                requestId,
                "DELIVERED",
                "HANDOVER",
                "Request delivered to business",
                currentUserId);
        }

        public async Task<bool> CloseAsync(int requestId, string currentUserId)
        {
            return await SetStatusAndStageAsync(
                requestId,
                "CLOSED",
                "COMPLETED",
                "Request closed",
                currentUserId);
        }

        public async Task<List<ProjectRequestDto>> GetAssignedToMeAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🔍 Getting requests assigned to user {UserId}", userId);

                var requests = await GetProjectRequestWithIncludes()
                    .Where(pr => pr.AssignedTo == userId)
                    .OrderByDescending(pr => pr.CreatedDate)
                    .ToListAsync();

                var dtos = new List<ProjectRequestDto>();
                foreach (var request in requests)
                {
                    dtos.Add(await MapToProjectRequestDto(request));
                }

                _logger.LogInformation("✅ Found {Count} requests assigned to user {UserId}", dtos.Count, userId);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests assigned to user {UserId}", userId);
                return new List<ProjectRequestDto>();
            }
        }


        public async Task<List<ProjectRequestDto>> GetByWorkflowStageAsync(int workflowStageConfigId)
        {
            try
            {
                _logger.LogInformation("🔍 Getting requests in workflow stage {StageId}", workflowStageConfigId);

                var requests = await GetProjectRequestWithIncludes()
                    .Where(pr => pr.WorkflowStageConfigId == workflowStageConfigId)
                    .OrderByDescending(pr => pr.CreatedDate)
                    .ToListAsync();

                var dtos = new List<ProjectRequestDto>();
                foreach (var request in requests)
                {
                    dtos.Add(await MapToProjectRequestDto(request));
                }

                _logger.LogInformation("✅ Found {Count} requests in workflow stage {StageId}", dtos.Count, workflowStageConfigId);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests for workflow stage {StageId}", workflowStageConfigId);
                return new List<ProjectRequestDto>();
            }
        }


        public async Task<List<ProjectRequestDto>> GetRequestsNeedingEvaluationAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Getting requests needing evaluation");

                var underEvaluationStatus = await _configService.GetStatusByNameAsync("Under Evaluation");
                if (underEvaluationStatus == null) return new List<ProjectRequestDto>();

                var requests = await GetProjectRequestWithIncludes()
                    .Where(pr => pr.StatusConfigId == underEvaluationStatus.Id &&
                                !pr.TotalScore.HasValue) // No evaluation scores yet
                    .OrderByDescending(pr => pr.CreatedDate)
                    .ToListAsync();

                var dtos = new List<ProjectRequestDto>();
                foreach (var request in requests)
                {
                    dtos.Add(await MapToProjectRequestDto(request));
                }

                _logger.LogInformation("✅ Found {Count} requests needing evaluation", dtos.Count);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests needing evaluation");
                return new List<ProjectRequestDto>();
            }
        }

        public async Task<List<ProjectRequestDto>> GetPendingApprovalAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Getting requests pending approval");

                var underEvaluationStatus = await _configService.GetStatusByNameAsync("Under Evaluation");
                if (underEvaluationStatus == null) return new List<ProjectRequestDto>();

                var requests = await GetProjectRequestWithIncludes()
                    .Where(pr => pr.StatusConfigId == underEvaluationStatus.Id &&
                                pr.TotalScore.HasValue) // Has evaluation scores but not approved yet
                    .OrderByDescending(pr => pr.CreatedDate)
                    .ToListAsync();

                var dtos = new List<ProjectRequestDto>();
                foreach (var request in requests)
                {
                    dtos.Add(await MapToProjectRequestDto(request));
                }

                _logger.LogInformation("✅ Found {Count} requests pending approval", dtos.Count);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting requests pending approval");
                return new List<ProjectRequestDto>();
            }
        }

        public async Task<bool> CreateAssignmentAsync(int requestId, CreateAssignmentDto assignmentDto, string currentUserId)
        {
            try
            {
                var request = await _context.ProjectRequests.FindAsync(requestId);
                if (request == null) return false;

                var assignment = new ProjectRequestAssignment
                {
                    ProjectRequestId = requestId,
                    AssigneeId = assignmentDto.AssigneeId,
                    AssigneeRole = assignmentDto.AssigneeRole,
                    AssignmentNotes = assignmentDto.AssignmentNotes,
                    AssignedDate = DateTime.UtcNow,
                    // CompletedDate will be set when task is done
                };

                _context.ProjectRequestAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment for request {RequestId}", requestId);
                return false;
            }
        }

       

        public async Task<List<StatusHistoryDto>> GetStatusHistoryAsync(int requestId)
        {
            return await _workflowService.GetStatusHistoryAsync(requestId);
        }

        public async Task<List<WorkflowHistoryDto>> GetWorkflowHistoryAsync(int requestId)
        {
            return await _workflowService.GetWorkflowHistoryAsync(requestId);
        }

        public Task<bool> CreateMultipleAssignmentsAsync(CreateMultipleAssignmentsDto assignmentsDto, string currentUserId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ProjectRequestAssignment>> GetAssignmentAsync(int requestId)
        {
           var assignments = await _context.ProjectRequestAssignments
                .Where(a => a.ProjectRequestId == requestId)
                .ToListAsync();

            return assignments ?? new List<ProjectRequestAssignment>();
        }


    

    }


}