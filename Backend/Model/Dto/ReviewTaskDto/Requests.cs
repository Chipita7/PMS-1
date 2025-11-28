using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ReviewTaskDto.Requests
{
    public class CreateManualTaskDto
    {
        [Required]
        [StringLength(50)]
        public string AssigneeId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AssigneeName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AssigneeRole { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(3);
    }

    public class UpdateTaskDto
    {
        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }
    }

    public class CompleteTaskDto
    {
        [StringLength(1000)]
        public string? CompletionRemarks { get; set; }
    }

    public class ReassignTaskDto
    {
        [Required]
        [StringLength(50)]
        public string NewAssigneeId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string NewAssigneeName { get; set; } = string.Empty;
    }

    public class GenerateTasksFromTemplatesDto
    {
        [Required]
        public List<string> TaskCodes { get; set; } = new List<string>();

        public Dictionary<string, string>? AssigneeOverrides { get; set; }

        [Range(1, 30)]
        public int? CustomDueDays { get; set; }
    }

    public class GenerateTasksForReviewerTypeDto
    {
        [Required]
        public string ReviewerType { get; set; } = string.Empty;

        [Required]
        public string AssigneeId { get; set; } = string.Empty;

        [Range(1, 30)]
        public int? CustomDueDays { get; set; }
    }
}