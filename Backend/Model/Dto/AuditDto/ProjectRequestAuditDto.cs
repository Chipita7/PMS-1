namespace ProjectManagementSystem1.Model.Dto.AuditDto
{
    public class ProjectRequestAuditDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedOn { get; set; }
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? EntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class AuditSummaryDto
    {
        public int TotalAuditEntries { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int StatusChangeCount { get; set; }
        public int OwnerChangeCount { get; set; }
        public DateTime? FirstActivity { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, int> ActivitiesByUser { get; set; } = new();
        public Dictionary<string, int> ActivitiesByType { get; set; } = new();
    }

    public class ChangeDetail
    {
        public string FieldName { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }
}
