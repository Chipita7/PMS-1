using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests
{
    public class UpdateProjectRequestDto
    {
        [Required]
        public int Id { get; set; }

        // Fields that can be updated
        [StringLength(200)]
        public string? RequestTitle { get; set; }

        public string? RequestDescription { get; set; }

        [StringLength(15)]
        public string? ReferenceNo { get; set; }

        // Workflow updates
        public int? StatusConfigId { get; set; }
        public int? WorkflowStageConfigId { get; set; }

        [StringLength(100)]
        public string? AssignedTeam { get; set; }

        [StringLength(50)]
        public string? AssignedTo { get; set; }

        // Evaluation updates
        [StringLength(50)]
        public string? EvaluatorID { get; set; }

        [Range(1, 10)]
        public int? FeasibilityScore { get; set; }

        [Range(1, 10)]
        public int? BusinessValueScore { get; set; }

        [Range(1, 10)]
        public int? TechnicalComplexityScore { get; set; }

        public string? EvaluationRemarks { get; set; }

        [StringLength(50)]
        public string? RequestedByName { get; set; }

        [StringLength(50)]
        public string? BusinessSector { get; set; }

        [StringLength(50)]
        public string? BusinessDivision { get; set; }

        [StringLength(50)]
        public string? BusinessDepartment { get; set; }

        [StringLength(30)]
        public string? OrganizationName { get; set; }

        [EmailAddress]
        [StringLength(30)]
        public string? PrimaryContactEmail { get; set; }

        [EmailAddress]
        [StringLength(30)]
        public string? SecondaryContactEmail { get; set; }

        [StringLength(15)]
        public string? PrimaryContactPhone { get; set; }

        [StringLength(15)]
        public string? SecondaryContactPhone { get; set; }

        public int? StrategicAlignmentConfigId { get; set; }

        [StringLength(100)]
        public string? StrategicAlignment { get; set; }
    }
}