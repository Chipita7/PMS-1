namespace ProjectManagementSystem1.Services.Attachments.Models
{
    public class AttachmentComplianceResult
    {
        public bool IsCompliant => !MissingCategories.Any();
        public List<string> MissingCategories { get; set; } = new();
    }
}

