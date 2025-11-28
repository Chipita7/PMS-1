using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.EvaluationDto
{
    

    public class EvaluationDetailDto
    {
        public int Id { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerRole { get; set; } = string.Empty;
        public bool IsPrimaryEvaluation { get; set; }
        public int? StrategicAlignmentScore { get; set; }
        public int? FeasibilityScore { get; set; }
        public int? BusinessValueScore { get; set; }
        public int? TechnicalComplexityScore { get; set; }
        public double? OverallScore { get; set; }
        public string? EvaluationRemarks { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public EvaluationStatus EvaluationStatus { get; set; }
        public int? AssignmentId { get; set; }

    }
}
