using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class AssignmentRoleConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // "Evaluator", "Approver", etc.

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // "EVALUATOR", "APPROVER"

        public int Priority { get; set; } = 999; // Lower number = higher priority

        public bool CanBeMultiple { get; set; } = false; // Can multiple users have this role?

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        [StringLength(500)]
        public string? Description { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [StringLength(50)]
        public string LastUpdatedBy { get; set; } = string.Empty;
    }
}