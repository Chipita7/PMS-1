using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Entities.RequestConfigs;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Enums;

namespace ProjectManagementSystem1.Services.Configuration
{
    public class DataMigrationService : IDataMigrationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(AppDbContext context, ILogger<DataMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> MigrateEnumDataToConfigAsync()
        {
            try
            {
                _logger.LogInformation("Starting enum to config migration...");

                // Migrate all enum types
                await MigrateStatusEnumsAsync();
                await MigratePriorityEnumsAsync();
                await MigrateRequestTypeEnumsAsync();
                await MigrateRequestCategoryEnumsAsync();        
                await MigrateServiceCategoryEnumsAsync();         
                await MigrateProductCategoryEnumsAsync();        
                await MigrateImpactUrgencyEnumsAsync();         
                await MigrateWorkflowStageEnumsAsync();
                await MigrateAssignmentRoleConfigsAsync();
                await MigrateReviewTaskConfigsAsync();
                await MigrateStrategicAlignmentConfigsAsync();

                await _context.SaveChangesAsync();
                _logger.LogInformation("Enum to config migration completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enum to config migration.");
                return false;
            }
        }
        private async Task MigrateStatusEnumsAsync()
        {
            if (await _context.StatusConfigs.AnyAsync()) return;

            var statusConfigs = new List<StatusConfig>
            {
                new() { Name = "Submitted", Category = "Active", SortOrder = 1, Code = "SUBMITTED" },
                new() { Name = "Under Evaluation", Category = "Active", SortOrder = 2, Code = "UNDER_EVALUATION" },
                new() { Name = "Approved", Category = "Completed", SortOrder = 3, Code = "APPROVED" },
                new() { Name = "Rejected", Category = "Cancelled", SortOrder = 4, Code = "REJECTED" },
                new() { Name = "Backlogged", Category = "Active", SortOrder = 5, Code = "BACKLOGGED" },
                new() { Name = "InProgress", Category = "Active", SortOrder = 6, Code = "IN_PROGRESS" },
                new() { Name = "Completed", Category = "Completed", SortOrder = 7, Code = "COMPLETED" },
                new() { Name = "Delivered", Category = "Completed", SortOrder = 8, Code = "DELIVERED" },
                new() { Name = "Closed", Category = "Completed", SortOrder = 9, Code = "CLOSED" }
            };

            await _context.StatusConfigs.AddRangeAsync(statusConfigs);
        }

        private async Task MigratePriorityEnumsAsync()
        {
            if (await _context.PriorityConfigs.AnyAsync()) return;

            var priorityConfigs = new List<PriorityConfig>
            {
                new() { Name = "P1", Code = "P1", Color = "#FF0000", SortOrder = 1 },
                new() { Name = "P2", Code = "P2", Color = "#FFA500", SortOrder = 2 },
                new() { Name = "P3", Code = "P3", Color = "#FFFF00", SortOrder = 3 },
                new() { Name = "P4", Code = "P4", Color = "#00FF00", SortOrder = 4 },
                new() { Name = "P5", Code = "P5", Color = "#0000FF", SortOrder = 5 }
            };

            await _context.PriorityConfigs.AddRangeAsync(priorityConfigs);
        }

        private async Task MigrateRequestTypeEnumsAsync()
        {
            // Always update existing configs to ensure Idea Refinement flags are set
            var existingConfigs = await _context.RequestTypeConfigs.ToListAsync();
            
            if (existingConfigs.Any())
            {
                // Update existing configs to ensure they trigger Idea Refinement
                foreach (var config in existingConfigs)
                {
                    config.RequiresIdeaRefinementAlways = true;
                    config.RequireIdeaRefinementIfHighRisk = true;
                    config.IdeaRefinementMinScore = 5.0m;
                }
                _logger.LogInformation("Updated {Count} existing request type configs to require Idea Refinement", existingConfigs.Count);
            }
            else
            {
                // Create new configs if none exist
                var requestTypeConfigs = new List<RequestTypeConfig>
                {
                    new() { Name = "New Development", Code = "NEW_DEV", SortOrder = 1, RequiresIdeaRefinementAlways = true, RequireIdeaRefinementIfHighRisk = true, IdeaRefinementMinScore = 5.0m },
                    new() { Name = "Enhancement", Code = "ENHANCEMENT", SortOrder = 2, RequiresIdeaRefinementAlways = true, RequireIdeaRefinementIfHighRisk = true, IdeaRefinementMinScore = 5.0m },
                    new() { Name = "Internal Integration", Code = "INT_INTEGRATION", SortOrder = 3, RequiresIdeaRefinementAlways = true, RequireIdeaRefinementIfHighRisk = true, IdeaRefinementMinScore = 5.0m },
                    new() { Name = "Third-Party Integration", Code = "EXT_INTEGRATION", SortOrder = 4, RequiresIdeaRefinementAlways = true, RequireIdeaRefinementIfHighRisk = true, IdeaRefinementMinScore = 5.0m }
                };

                await _context.RequestTypeConfigs.AddRangeAsync(requestTypeConfigs);
                _logger.LogInformation("Created {Count} new request type configs with Idea Refinement enabled", requestTypeConfigs.Count);
            }
        }

        public async Task<bool> MigrateExistingProjectRequestsAsync()
        {
            try
            {
                _logger.LogInformation("Starting ProjectRequest data migration...");

                // Get all existing ProjectRequests that need migration
                var requests = await _context.ProjectRequests
                    .Where(pr => pr.StatusConfigId == 0) // Assuming default is 0
                    .ToListAsync();

                foreach (var request in requests)
                {
                    // For now, set default values
                    // In real scenario, you'd map from old enum values to new config IDs
                    var defaultStatus = await _context.StatusConfigs.FirstAsync(s => s.Code == "SUBMITTED");
                    var defaultPriority = await _context.PriorityConfigs.FirstAsync(p => p.Code == "P3");
                    var defaultRequestType = await _context.RequestTypeConfigs.FirstAsync(rt => rt.Code == "NEW_DEV");

                    request.StatusConfigId = defaultStatus.Id;
                    request.PriorityConfigId = defaultPriority.Id;
                    request.RequestTypeConfigId = defaultRequestType.Id;

                    // Add other config mappings as needed...
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Migrated {requests.Count} ProjectRequest records.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ProjectRequest migration.");
                return false;
            }
        }

        // Add these methods to your existing DataMigrationService class
        private async Task MigrateRequestCategoryEnumsAsync()
        {
            if (await _context.RequestCategoryConfigs.AnyAsync()) return;

            var requestCategoryConfigs = new List<RequestCategoryConfig>
    {
        new() { Name = "Remittance", Code = "REMITTANCE", SortOrder = 1 },
        new() { Name = "Government", Code = "GOVERNMENT", SortOrder = 2 },
        new() { Name = "Inhouse-development", Code = "INHOUSE_DEV", SortOrder = 3 }
    };

            await _context.RequestCategoryConfigs.AddRangeAsync(requestCategoryConfigs);
            _logger.LogInformation("Created {Count} request category configs", requestCategoryConfigs.Count);
        }

        private async Task MigrateServiceCategoryEnumsAsync()
        {
            if (await _context.ServiceCategoryConfigs.AnyAsync()) return;

            var serviceCategoryConfigs = new List<ServiceCategoryConfig>
    {
        new() { Name = "Digital Banking Management", Code = "DIGITAL_BANKING", SortOrder = 1 },
        new() { Name = "Card Banking Management", Code = "CARD_BANKING", SortOrder = 2 },
        new() { Name = "Credit Management", Code = "CREDIT_MGMT", SortOrder = 3 },
        new() { Name = "Risk Management", Code = "RISK_MGMT", SortOrder = 4 }
    };

            await _context.ServiceCategoryConfigs.AddRangeAsync(serviceCategoryConfigs);
            _logger.LogInformation("Created {Count} service category configs", serviceCategoryConfigs.Count);
        }

        private async Task MigrateProductCategoryEnumsAsync()
        {
            if (await _context.ProductCategoryConfigs.AnyAsync()) return;

            var productCategoryConfigs = new List<ProductCategoryConfig>
    {
        new() { Name = "Mobile Banking", Code = "MOBILE_BANKING", SortOrder = 1 },
        new() { Name = "CBE Birr Wallet", Code = "CBE_BIRR", SortOrder = 2 },
        new() { Name = "T24 Core Banking", Code = "T24_CORE", SortOrder = 3 },
        new() { Name = "IMAL Core Banking", Code = "IMAL_CORE", SortOrder = 4 },
        new() { Name = "Other Legacy System", Code = "OTHER_LEGACY", SortOrder = 5 }
    };

            await _context.ProductCategoryConfigs.AddRangeAsync(productCategoryConfigs);
            _logger.LogInformation("Created {Count} product category configs", productCategoryConfigs.Count);
        }

        private async Task MigrateImpactUrgencyEnumsAsync()
        {
            if (await _context.ImpactUrgencyConfigs.AnyAsync()) return;

            var impactUrgencyConfigs = new List<ImpactUrgencyConfig>
    {
        new() { Name = "High", Code = "HIGH", Color = "#FF0000", SortOrder = 1 },
        new() { Name = "Medium", Code = "MEDIUM", Color = "#FFA500", SortOrder = 2 },
        new() { Name = "Low", Code = "LOW", Color = "#00FF00", SortOrder = 3 }
    };

            await _context.ImpactUrgencyConfigs.AddRangeAsync(impactUrgencyConfigs);
            _logger.LogInformation("Created {Count} impact/urgency configs", impactUrgencyConfigs.Count);
        }
        public async Task<MigrationReport> GetMigrationReportAsync()
        {
            var report = new MigrationReport
            {
                MigrationDate = DateTime.UtcNow,
                TotalConfigEntriesCreated = await _context.StatusConfigs.CountAsync() +
                                          await _context.PriorityConfigs.CountAsync() +
                                          await _context.RequestTypeConfigs.CountAsync(),
                TotalProjectRequestsMigrated = await _context.ProjectRequests.CountAsync(pr => pr.StatusConfigId > 0)
            };

            return report;
        }

        private async Task MigrateWorkflowStageEnumsAsync()
        {
            if (await _context.WorkflowStageConfigs.AnyAsync())
            {
                _logger.LogInformation("WorkflowStageConfigs already exist, skipping migration");
                return;
            }

            var workflowStageConfigs = new List<WorkflowStageConfig>
    {
        new() { Name = "Submitted", Code = "SUBMITTED", Description = "Request has been submitted", SortOrder = 1, IsActive = true },
        new() { Name = "Initial Evaluation", Code = "INITIAL_EVAL", Description = "Request is under initial evaluation", SortOrder = 2, IsActive = true },
        
        // ✅ ADD THIS NEW STAGE
        new() { Name = "Approval Ready", Code = "APPROVAL_READY", Description = "All review tasks completed, ready for final approval", SortOrder = 3, IsActive = true },

        new() { Name = "Idea Refinement", Code = "IDEA_REFINEMENT", Description = "Request requirements are being refined", SortOrder = 4, IsActive = true },
        new() { Name = "Approved", Code = "APPROVED_STAGE", Description = "Request has been approved", SortOrder = 5, IsActive = true },
        new() { Name = "Sprint Planning", Code = "SPRINT_PLANNING", Description = "Request is in sprint planning", SortOrder = 6, IsActive = true },
        new() { Name = "In Development", Code = "IN_DEVELOPMENT", Description = "Request is being developed", SortOrder = 7, IsActive = true },
        new() { Name = "UAT", Code = "UAT", Description = "User acceptance testing", SortOrder = 8, IsActive = true },
        new() { Name = "Deployment", Code = "DEPLOYMENT", Description = "Request is being deployed", SortOrder = 9, IsActive = true },
        new() { Name = "Handover", Code = "HANDOVER", Description = "Request is being handed over to operations", SortOrder = 10, IsActive = true },
        new() { Name = "Completed", Code = "COMPLETED", Description = "Request is completed", SortOrder = 11, IsActive = true }
    };

            await _context.WorkflowStageConfigs.AddRangeAsync(workflowStageConfigs);
            _logger.LogInformation("Created {Count} workflow stage configs", workflowStageConfigs.Count);
        }

        public async Task<bool> ResetAndRegenerateRolesAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Resetting and regenerating roles...");

                // 1. Remove all existing roles
                var existingRoles = await _context.AssignmentRoleConfigs.ToListAsync();
                _context.AssignmentRoleConfigs.RemoveRange(existingRoles);

                // 2. Regenerate with document-compliant roles only
                await MigrateAssignmentRoleConfigsAsync();

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully reset and regenerated document-compliant roles");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resetting roles");
                return false;
            }
        }
        private async Task MigrateAssignmentRoleConfigsAsync()
        {
            var existingRoles = await _context.AssignmentRoleConfigs.ToListAsync();
            if (existingRoles.Any())
            {
                _context.AssignmentRoleConfigs.RemoveRange(existingRoles);
                await _context.SaveChangesAsync();
                _logger.LogInformation("🧹 Removed {Count} existing roles to start fresh", existingRoles.Count);
            }

            // ✅ ONLY document-specified roles
            var roleConfigs = new List<AssignmentRoleConfig>
    {
        new() {
            Name = "Head of Product Management",
            Code = "HEAD_PRODUCT_MGMT",
            Priority = 1,
            CanBeMultiple = false, // Only one Head of PM
            Description = "Leads review of business requests, assesses strategic alignment, creates tasks and assigns to reviewers",
            SortOrder = 1
        },
        new() {
            Name = "Business Analyst",
            Code = "BUSINESS_ANALYST",
            Priority = 2,
            CanBeMultiple = true, // Can have multiple BAs
            Description = "Gathers and clarifies business requests, conducts evaluation, documents product objectives and user stories",
            SortOrder = 2
        },
        new() {
            Name = "Innovation Chapter",
            Code = "INNOVATION_CHAPTER",
            Priority = 3,
            CanBeMultiple = true,
            Description = "Provides insights into innovative approaches and opportunities for enhancement",
            SortOrder = 3
        },
        new() {
            Name = "Subject Matter Expert",
            Code = "SUBJECT_MATTER_EXPERT",
            Priority = 4,
            CanBeMultiple = true,
            Description = "Offers domain-specific expertise",
            SortOrder = 4
        },
        new() {
            Name = "Stakeholder",
            Code = "STAKEHOLDER",
            Priority = 5,
            CanBeMultiple = true,
            Description = "Business stakeholders providing initial input and feedback",
            SortOrder = 5
        },
        new() {
            Name = "Head of Engineering",
            Code = "HEAD_ENGINEERING",
            Priority = 6,
            CanBeMultiple = false,
            Description = "Provides technical insights into feasibility and helps refine the idea",
            SortOrder = 6
        },
        new() {
            Name = "Information Security",
            Code = "INFO_SECURITY",
            Priority = 7,
            CanBeMultiple = true,
            Description = "Identifies security concerns early on",
            SortOrder = 7
        },
        // ✅ MISSING IDEA REFINEMENT ROLES FROM DOCUMENT
        new() {
            Name = "Head of Data & AI",
            Code = "HEAD_DATA_AI",
            Priority = 8,
            CanBeMultiple = false,
            Description = "Ensures data-related considerations are integrated into the refinement process",
            SortOrder = 8
        },
        new() {
            Name = "Product Owner",
            Code = "PRODUCT_OWNER",
            Priority = 9,
            CanBeMultiple = true,
            Description = "Collaborates to break down the idea into user stories and align with development needs",
            SortOrder = 9
        },
        new() {
            Name = "DevSecOps Team",
            Code = "DEVSECOPS_TEAM",
            Priority = 10,
            CanBeMultiple = true,
            Description = "Contributes automation and security feasibility insights (CI/CD potential), supporting IS Division and Head of Engineering",
            SortOrder = 10
        },
        // ✅ APPROVAL/REVIEW ROLES
        new() {
            Name = "VP/Director",
            Code = "VP_DIRECTOR",
            Priority = 11,
            CanBeMultiple = false,
            Description = "Reviews and approves requests for final approval",
            SortOrder = 11
        },
        new() {
            Name = "Compliance Officer",
            Code = "COMPLIANCE_OFFICER",
            Priority = 12,
            CanBeMultiple = false,
            Description = "Ensures regulatory and compliance requirements are met",
            SortOrder = 12
        },
        new() {
            Name = "PMO",
            Code = "PMO",
            Priority = 13,
            CanBeMultiple = false,
            Description = "Project Management Office oversight and strategic alignment review",
            SortOrder = 13
        }
    };

            await _context.AssignmentRoleConfigs.AddRangeAsync(roleConfigs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} document-specified role configs", roleConfigs.Count);
        }

        public async Task<bool> EnsureWorkflowStagesExistAsync()
        {
            try
            {
                if (!await _context.WorkflowStageConfigs.AnyAsync())
                {
                    _logger.LogInformation("No workflow stages found, creating default stages...");
                    await MigrateWorkflowStageEnumsAsync();
                    await _context.SaveChangesAsync();
                    return true;
                }

                _logger.LogInformation("Workflow stages already exist");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring workflow stages exist");
                return false;
            }
        }

        private async Task MigrateReviewTaskConfigsAsync()
        {
            var existingTasks = await _context.ReviewTaskConfigs.ToListAsync();
            if (existingTasks.Any())
            {
                _context.ReviewTaskConfigs.RemoveRange(existingTasks);
                await _context.SaveChangesAsync();
            }

            var taskConfigs = new List<ReviewTaskConfig>
    {
        new()
        {
            Name = "Business Requirements Analysis",
            Code = "BUSINESS_REQUIREMENTS",
            DescriptionTemplate = "Gather and clarify business requirements for {RequestTitle} - Document user stories and acceptance criteria",
            AssigneeRole = "Business Analyst", // ✅ CHANGED from "BusinessAnalyst" to "Business Analyst"
            DefaultDueDays = 5,
            SortOrder = 1,
            ApplicableRequestTypes = "NEW_DEV,ENHANCEMENT"
        },
        new()
        {
            Name = "Technical Feasibility Assessment",
            Code = "TECH_FEASIBILITY",
            DescriptionTemplate = "Assess technical feasibility and implementation complexity for {RequestTitle}",
            AssigneeRole = "Head of Engineering", // ✅ CHANGED from "TechnicalLead" to "Head of Engineering"
            DefaultDueDays = 4,
            SortOrder = 2,
            ApplicableRequestTypes = "NEW_DEV,ENHANCEMENT,INT_INTEGRATION,EXT_INTEGRATION"
        },
        new()
        {
            Name = "Innovation Insights",
            Code = "INNOVATION_INSIGHTS",
            DescriptionTemplate = "Provide innovative approaches and enhancement opportunities for {RequestTitle}",
            AssigneeRole = "Innovation Chapter", // ✅ CHANGED from "InnovationChapter" to "Innovation Chapter"
            DefaultDueDays = 3,
            SortOrder = 3
        },
        new()
        {
            Name = "Security Impact Analysis",
            Code = "SECURITY_ANALYSIS",
            DescriptionTemplate = "Review security implications and requirements for {RequestTitle}",
            AssigneeRole = "Information Security", // ✅ CHANGED from "InformationSecurity" to "Information Security"
            DefaultDueDays = 5,
            SortOrder = 4,
            ApplicableRequestTypes = "NEW_DEV,EXT_INTEGRATION"
        },
        new()
        {
            Name = "Cost-Benefit Analysis",
            Code = "COST_BENEFIT",
            DescriptionTemplate = "Analyze cost implications and business value for {RequestTitle}",
            AssigneeRole = "Business Analyst", // ✅ CHANGED from "BusinessAnalyst" to "Business Analyst"
            DefaultDueDays = 4,
            SortOrder = 5
        },
        // ✅ IDEA REFINEMENT STAGE REVIEW TASKS (from document requirements)
        new()
        {
            Name = "Data & AI Integration Review",
            Code = "DATA_AI_REVIEW",
            DescriptionTemplate = "Ensure data-related considerations are integrated into the refinement process for {RequestTitle}",
            AssigneeRole = "Head of Data & AI",
            DefaultDueDays = 4,
            SortOrder = 6,
            ApplicableRequestTypes = "NEW_DEV,ENHANCEMENT,INT_INTEGRATION"
        },
        new()
        {
            Name = "Product Breakdown Review",
            Code = "PRODUCT_BREAKDOWN_REVIEW",
            DescriptionTemplate = "Break down the idea into user stories and align with development needs for {RequestTitle}",
            AssigneeRole = "Product Owner",
            DefaultDueDays = 5,
            SortOrder = 7,
            ApplicableRequestTypes = "NEW_DEV,ENHANCEMENT"
        },
        new()
        {
            Name = "DevSecOps Feasibility Review",
            Code = "DEVSECOPS_REVIEW",
            DescriptionTemplate = "Contribute automation and security feasibility insights (CI/CD potential) for {RequestTitle}",
            AssigneeRole = "DevSecOps Team",
            DefaultDueDays = 3,
            SortOrder = 8,
            ApplicableRequestTypes = "NEW_DEV,ENHANCEMENT,INT_INTEGRATION,EXT_INTEGRATION"
        },
        new()
        {
            Name = "Subject Matter Expert Refinement",
            Code = "SME_REFINEMENT_REVIEW",
            DescriptionTemplate = "Provide specific domain insights to refine the idea further for {RequestTitle}",
            AssigneeRole = "Subject Matter Expert",
            DefaultDueDays = 4,
            SortOrder = 9
        },
        // ✅ FINAL APPROVAL REVIEW TASKS
        new()
        {
            Name = "VP/Director Final Approval",
            Code = "VP_DIRECTOR_APPROVAL",
            DescriptionTemplate = "Review and provide final executive approval decision for {RequestTitle}",
            AssigneeRole = "VP/Director",
            DefaultDueDays = 2,
            SortOrder = 10
        },
        new()
        {
            Name = "Compliance Final Review",
            Code = "COMPLIANCE_APPROVAL",
            DescriptionTemplate = "Ensure all regulatory and compliance requirements are satisfied for {RequestTitle}",
            AssigneeRole = "Compliance Officer",
            DefaultDueDays = 3,
            SortOrder = 11,
            ApplicableRequestTypes = "NEW_DEV,EXT_INTEGRATION"
        },
        new()
        {
            Name = "PMO Strategic Alignment Review",
            Code = "PMO_APPROVAL",
            DescriptionTemplate = "Review strategic alignment and resource allocation for {RequestTitle}",
            AssigneeRole = "PMO",
            DefaultDueDays = 2,
            SortOrder = 12
        }
    };

            await _context.ReviewTaskConfigs.AddRangeAsync(taskConfigs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} review task configs", taskConfigs.Count);
        }

        private async Task MigrateStrategicAlignmentConfigsAsync()
        {
            var existingAlignments = await _context.StrategicAlignmentConfigs.ToListAsync();
            if (existingAlignments.Any())
            {
                _context.StrategicAlignmentConfigs.RemoveRange(existingAlignments);
                await _context.SaveChangesAsync();
            }

            var alignmentConfigs = new List<StrategicAlignmentConfig>
            {
                new() {
                    Name = "Digital Transformation",
                    Code = "DIGITAL_TRANSFORM",
                    Description = "Initiatives supporting digital transformation strategy",
                    IsActive = true,
                    SortOrder = 1
                },
                new() {
                    Name = "Customer Experience Enhancement",
                    Code = "CUSTOMER_EXPERIENCE",
                    Description = "Projects focused on improving customer experience",
                    IsActive = true,
                    SortOrder = 2
                },
                new() {
                    Name = "Operational Efficiency",
                    Code = "OPERATIONAL_EFFICIENCY",
                    Description = "Initiatives to improve operational efficiency and reduce costs",
                    IsActive = true,
                    SortOrder = 3
                },
                new() {
                    Name = "Risk Management & Compliance",
                    Code = "RISK_COMPLIANCE",
                    Description = "Projects supporting risk management and regulatory compliance",
                    IsActive = true,
                    SortOrder = 4
                },
                new() {
                    Name = "Innovation & Growth",
                    Code = "INNOVATION_GROWTH",
                    Description = "Strategic initiatives for innovation and business growth",
                    IsActive = true,
                    SortOrder = 5
                },
                new() {
                    Name = "Infrastructure Modernization",
                    Code = "INFRASTRUCTURE_MODERN",
                    Description = "Technology infrastructure modernization and upgrade projects",
                    IsActive = true,
                    SortOrder = 6
                }
            };

            await _context.StrategicAlignmentConfigs.AddRangeAsync(alignmentConfigs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} strategic alignment configs", alignmentConfigs.Count);
        }

    }
}