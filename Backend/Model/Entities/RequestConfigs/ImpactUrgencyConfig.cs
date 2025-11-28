using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class ImpactUrgencyConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; } = string.Empty; // "High", "Medium", "Low"

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty; // "HIGH", "MEDIUM", "LOW"

        [StringLength(10)]
        public string? Color { get; set; } // "#FF0000", "green"

        [StringLength(100)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int SortOrder { get; set; } = 1;

        // Navigation properties for different usages
        public virtual ICollection<ProjectRequest> BusinessImpactConfigRequests { get; set; } = new List<ProjectRequest>();
        public virtual ICollection<ProjectRequest> RequestUrgencyConfigRequests { get; set; } = new List<ProjectRequest>();
        public virtual ICollection<ProjectRequest> RiskLevelConfigRequests { get; set; } = new List<ProjectRequest>();
        public virtual ICollection<ProjectRequest> ComplexityLevelConfigRequests { get; set; } = new List<ProjectRequest>();
    }
}