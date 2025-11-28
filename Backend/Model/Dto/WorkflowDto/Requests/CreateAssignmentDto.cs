using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.WorkflowDto.Requests
{
    public class CreateAssignmentDto
    {
        [Required]
        [StringLength(50)]
        public string AssigneeId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AssigneeRole { get; set; } = string.Empty; // "Evaluator", "Approver", "Developer", "Tester"

        [StringLength(500)]
        public string AssignmentNotes { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }
    }

    public class CreateMultipleAssignmentsDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateAssignmentDto> Assignments { get; set; } = new();
    }
}
