using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class StatusConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // "Submitted", "Under Evaluation"

        [Required] // ADD THIS
        [StringLength(20)]
        public string Code { get; set; } = string.Empty; // "SUBMITTED", "UNDER_EVALUATION"

        [Required]
        [StringLength(20)]
        public string Category { get; set; } = "Active"; // "Active", "Completed", "Cancelled"

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int SortOrder { get; set; } = 1;

        // Navigation property
        public virtual ICollection<ProjectRequest> ProjectRequests { get; set; } = new List<ProjectRequest>();
    }
}