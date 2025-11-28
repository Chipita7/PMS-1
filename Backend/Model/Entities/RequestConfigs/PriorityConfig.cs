using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class PriorityConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; } = string.Empty; // "P1", "P2", "P3"

        [Required] // ADD THIS
        [StringLength(20)]
        public string Code { get; set; } = string.Empty; // "P1", "P2", "P3"

        [StringLength(10)]
        public string? Color { get; set; } // "#FF0000", "red"

        [StringLength(100)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int SortOrder { get; set; } = 1;

        // Navigation property
        public virtual ICollection<ProjectRequest> ProjectRequests { get; set; } = new List<ProjectRequest>();
    }
}