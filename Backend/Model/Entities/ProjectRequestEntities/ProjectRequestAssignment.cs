using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Entities.ProjectRequestEntities
{
    public class ProjectRequestAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectRequestId { get; set; }

        [ForeignKey("ProjectRequestId")]
        public virtual ProjectRequest ProjectRequest { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string AssigneeId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AssigneeRole { get; set; } = string.Empty; // "Evaluator", "Approver", "Developer"

        [StringLength(50)]
        public string? ReviewerType { get; set; }

        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedDate { get; set; }

        [StringLength(500)]
        public string? AssignmentNotes { get; set; }

        public bool IsPrimary { get; set; } = false;
        public bool IsPrimaryEvaluator { get; set; } = false;
        public int AssignmentWeight { get; set; } = 50;

    }
}
