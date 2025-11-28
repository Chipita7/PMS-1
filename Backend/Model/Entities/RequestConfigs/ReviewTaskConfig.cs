using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Entities.RequestConfigs
{
    public class ReviewTaskConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DescriptionTemplate { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AssigneeRole { get; set; } = string.Empty; // "BusinessAnalyst", "TechnicalLead", etc.

        public int DefaultDueDays { get; set; } = 3;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Which request types/categories trigger this task?
        public string? ApplicableRequestTypes { get; set; } // "NEW_DEV,ENHANCEMENT" or null for all
        public string? ApplicableCategories { get; set; } // "REMITTANCE,GOVERNMENT" or null for all

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedDate { get; set; }
    }
}
