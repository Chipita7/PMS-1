using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Model.Dto.AuditDto;
using ProjectManagementSystem1.Data;

namespace ProjectManagementSystem1.Services.Audit
{
    

    public class ProjectRequestAuditService : IProjectRequestAuditService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProjectRequestAuditService> _logger;

        public ProjectRequestAuditService(AppDbContext context, ILogger<ProjectRequestAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogCreationAsync(int projectRequestId, string createdBy, object initialData, string? ipAddress = null)
        {
            var audit = new ProjectRequestAudit
            {
                ProjectRequestId = projectRequestId,
                Action = "Created",
                Description = $"Project request created by {createdBy}",
                ChangedBy = createdBy,
                ChangedOn = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(initialData),
                IpAddress = ipAddress
            };

            _context.ProjectRequestAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogUpdateAsync(int projectRequestId, string updatedBy, Dictionary<string, ChangeDetail> changes, string? ipAddress = null)
        {
            if (!changes.Any()) return;

            foreach (var change in changes)
            {
                var audit = new ProjectRequestAudit
                {
                    ProjectRequestId = projectRequestId,
                    Action = "Updated",
                    Description = $"{change.Key} updated by {updatedBy}",
                    ChangedBy = updatedBy,
                    ChangedOn = DateTime.UtcNow,
                    FieldName = change.Key,
                    OldValue = change.Value.OldValue?.ToString(),
                    NewValue = change.Value.NewValue?.ToString(),
                    IpAddress = ipAddress
                };

                _context.ProjectRequestAudits.Add(audit);
            }

            await _context.SaveChangesAsync();
        }

        public async Task LogStatusChangeAsync(int projectRequestId, string changedBy, string oldStatus, string newStatus, string? ipAddress = null)
        {
            var audit = new ProjectRequestAudit
            {
                ProjectRequestId = projectRequestId,
                Action = "StatusChanged",
                Description = $"Status changed from {oldStatus} to {newStatus} by {changedBy}",
                ChangedBy = changedBy,
                ChangedOn = DateTime.UtcNow,
                FieldName = "Status",
                OldValue = oldStatus,
                NewValue = newStatus,
                IpAddress = ipAddress
            };

            _context.ProjectRequestAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogOwnerChangeAsync(int projectRequestId, string changedBy, string oldOwner, string newOwner, string? ipAddress = null)
        {
            var audit = new ProjectRequestAudit
            {
                ProjectRequestId = projectRequestId,
                Action = "OwnerChanged",
                Description = $"Owner changed from {oldOwner} to {newOwner} by {changedBy}",
                ChangedBy = changedBy,
                ChangedOn = DateTime.UtcNow,
                FieldName = "Owner",
                OldValue = oldOwner,
                NewValue = newOwner,
                IpAddress = ipAddress
            };

            _context.ProjectRequestAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogWorkflowActionAsync(int projectRequestId, string actionBy, string workflowAction, string details, string? ipAddress = null)
        {
            var audit = new ProjectRequestAudit
            {
                ProjectRequestId = projectRequestId,
                Action = "WorkflowAction",
                Description = $"{workflowAction} performed by {actionBy}: {details}",
                ChangedBy = actionBy,
                ChangedOn = DateTime.UtcNow,
                AdditionalData = details,
                IpAddress = ipAddress
            };

            _context.ProjectRequestAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task LogEntityActionAsync(int projectRequestId, string actionBy, string entityType, int entityId, string action, string description, string? ipAddress = null)
        {
            var audit = new ProjectRequestAudit
            {
                ProjectRequestId = projectRequestId,
                Action = action,
                Description = description,
                ChangedBy = actionBy,
                ChangedOn = DateTime.UtcNow,
                EntityType = entityType,
                RelatedEntityId = entityId,
                IpAddress = ipAddress
            };

            _context.ProjectRequestAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProjectRequestAuditDto>> GetAuditTrailAsync(int projectRequestId)
        {
            var audits = await _context.ProjectRequestAudits
                .Where(a => a.ProjectRequestId == projectRequestId)
                .OrderByDescending(a => a.ChangedOn)
                .ToListAsync();

            return audits.Select(MapToDto).ToList();
        }

        public async Task<AuditSummaryDto> GetAuditSummaryAsync(int projectRequestId)
        {
            var audits = await _context.ProjectRequestAudits
                .Where(a => a.ProjectRequestId == projectRequestId)
                .ToListAsync();

            var summary = new AuditSummaryDto
            {
                TotalAuditEntries = audits.Count,
                CreatedCount = audits.Count(a => a.Action == "Created"),
                UpdatedCount = audits.Count(a => a.Action == "Updated"),
                StatusChangeCount = audits.Count(a => a.Action == "StatusChanged"),
                OwnerChangeCount = audits.Count(a => a.Action == "OwnerChanged"),
                FirstActivity = audits.Any() ? audits.Min(a => a.ChangedOn) : null,
                LastActivity = audits.Any() ? audits.Max(a => a.ChangedOn) : null,
                ActivitiesByUser = audits.GroupBy(a => a.ChangedBy)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ActivitiesByType = audits.GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return summary;
        }

        public async Task<List<ProjectRequestAuditDto>> GetRecentActivityAsync(int projectRequestId, int count = 50)
        {
            var audits = await _context.ProjectRequestAudits
                .Where(a => a.ProjectRequestId == projectRequestId)
                .OrderByDescending(a => a.ChangedOn)
                .Take(count)
                .ToListAsync();

            return audits.Select(MapToDto).ToList();
        }

        private ProjectRequestAuditDto MapToDto(ProjectRequestAudit audit)
        {
            return new ProjectRequestAuditDto
            {
                Id = audit.Id,
                ProjectRequestId = audit.ProjectRequestId,
                Action = audit.Action,
                Description = audit.Description,
                ChangedBy = audit.ChangedBy,
                ChangedOn = audit.ChangedOn,
                FieldName = audit.FieldName,
                OldValue = audit.OldValue,
                NewValue = audit.NewValue,
                EntityType = audit.EntityType,
                RelatedEntityId = audit.RelatedEntityId,
                AdditionalData = audit.AdditionalData
            };
        }
    }
}