using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Services.Attachments.Models;
using RequestAttachmentCategory = ProjectManagementSystem1.Models.Enums.AttachmentCategory;

namespace ProjectManagementSystem1.Services.Attachments
{
    public class AttachmentComplianceService : IAttachmentComplianceService
    {
        // ✅ FIXED: No attachments are required by default - all are optional
        // Attachments will only be required if explicitly marked as mandatory in the request/config
        private static readonly RequestAttachmentCategory[] RequiredCategories = Array.Empty<RequestAttachmentCategory>();
        
        // All categories are optional/recommended
        private static readonly RequestAttachmentCategory[] RecommendedCategories =
        {
            RequestAttachmentCategory.BusinessCase,
            RequestAttachmentCategory.BusinessRequirementDocument,
            RequestAttachmentCategory.RequestMemo,
            RequestAttachmentCategory.BusinessFeasibilityReport,
            RequestAttachmentCategory.TechnicalFeasibilityReport,
            RequestAttachmentCategory.CostEstimates,
            RequestAttachmentCategory.SecurityClearance
        };

        private readonly AppDbContext _context;
        private readonly ILogger<AttachmentComplianceService> _logger;

        public AttachmentComplianceService(AppDbContext context, ILogger<AttachmentComplianceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AttachmentComplianceResult> ValidateAsync(int projectRequestId)
        {
            var attachments = await _context.ProjectRequestAttachments
                .Where(a => a.ProjectRequestId == projectRequestId)
                .ToListAsync();

            var missing = new List<string>();

            foreach (var category in RequiredCategories)
            {
                if (!attachments.Any(a => a.FileCategory == category))
                {
                    missing.Add(category.ToString());
                }
            }

            if (missing.Any())
            {
                _logger.LogWarning("Attachment compliance failed for request {RequestId}. Missing categories: {Missing}",
                    projectRequestId, string.Join(", ", missing));
            }

            return new AttachmentComplianceResult
            {
                MissingCategories = missing
            };
        }
    }
}

