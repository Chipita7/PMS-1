using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses
{
    public class AssignmentRoleDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        public int Priority { get; set; }

        public bool CanBeMultiple { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public string? Description { get; set; }
    }
}

