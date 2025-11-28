using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class RequestTypeConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // "New Development", "Enhancement"

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty; // "NEW_DEV", "ENHANCEMENT"

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int SortOrder { get; set; } = 1;

        public bool RequiresIdeaRefinementAlways { get; set; } = false;

        public decimal? IdeaRefinementMinScore { get; set; }

        public decimal? IdeaRefinementMaxScore { get; set; }

        public bool RequireIdeaRefinementIfHighRisk { get; set; } = false;

        // Navigation property
        public virtual ICollection<ProjectRequest> ProjectRequests { get; set; } = new List<ProjectRequest>();
    }
}