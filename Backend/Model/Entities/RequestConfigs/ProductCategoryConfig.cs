using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class ProductCategoryConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // "Mobile Banking", "CBE Birr Wallet"

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty; // "MOBILE_BANKING", "CBE_BIRR"

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int SortOrder { get; set; } = 1;

        public virtual ICollection<ProjectRequest> ProjectRequests { get; set; } = new List<ProjectRequest>();
    }
}