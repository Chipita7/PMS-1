// Models/Entities/ProjectRequestEntities/ProjectRequestAudit.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class ProjectRequestAudit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectRequestId { get; set; }

        [ForeignKey("ProjectRequestId")]
        public virtual ProjectRequest ProjectRequest { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, StatusChanged, OwnerChanged, etc.

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ChangedBy { get; set; } = string.Empty; // AD User

        public DateTime ChangedOn { get; set; } = DateTime.UtcNow;

        // For tracking field-level changes
        [StringLength(100)]
        public string? FieldName { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        // Additional context
        [StringLength(50)]
        public string? EntityType { get; set; } // Request, Comment, Feedback, etc.

        public int? RelatedEntityId { get; set; } // ID of related entity if applicable

        public string? AdditionalData { get; set; } // JSON for complex changes

        // IP Address for security auditing (optional)
        [StringLength(45)]
        public string? IpAddress { get; set; }
    }
}