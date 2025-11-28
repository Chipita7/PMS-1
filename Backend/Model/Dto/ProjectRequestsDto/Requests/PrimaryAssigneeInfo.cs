using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests
{
    public class PrimaryAssigneeInfo
    {
        public string? PrimaryAssigneeId { get; set; }
        public string? PrimaryAssigneeRole { get; set; }
        public string? EvaluatorId { get; set; }
        public List<string> EvaluatorIds { get; set; } = new();
        public int ActiveAssignmentsCount { get; set; }
        public List<ProjectRequestAssignment> AllAssignments { get; set; } = new();
        public List<AssignmentRoleConfig> RoleConfigurations { get; set; } = new();
    }

    public class SetPrimaryAssigneeDto
    {
        [Required]
        public string AssigneeId { get; set; } = string.Empty;
    }

    public class SetPrimaryEvaluatorDto
    {
        [Required]
        public string EvaluatorId { get; set; } = string.Empty;
    }

    public class AssignRequestDto
    {
        [Required]
        public string AssignedTeam { get; set; } = string.Empty;

        [Required]
        public string AssignedTo { get; set; } = string.Empty;

        [Required]
        public string AssigneeRole { get; set; } = "Team Member";

        [StringLength(50)]
        public string? ReviewerType { get; set; }
        public string? AssignmentNotes { get; set; }

        public bool SetAsPrimary { get; set; } = false; // ✅ NEW: Option to set as primary immediately
        
        public bool AutoCreateTasks { get; set; } = true; // ✅ NEW: Option to auto-create review tasks (default: true for backward compatibility)
    }
}
