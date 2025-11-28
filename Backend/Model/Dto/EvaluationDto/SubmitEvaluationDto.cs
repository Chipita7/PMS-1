using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.EvaluationDto
{
    public class SubmitEvaluationDto
    {
        // ✅ FIXED: Per requirements, StrategicAlignmentScore is optional (StrategicAlignment is a lookup field, not a score)
        [Range(0, 10, ErrorMessage = "Strategic Alignment Score must be between 0 and 10 (0 = not provided)")]
        public int? StrategicAlignmentScore { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Feasibility Score must be between 1 and 10")]
        public int FeasibilityScore { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Business Value Score must be between 1 and 10")]
        public int BusinessValueScore { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Technical Complexity Score must be between 1 and 10")]
        public int TechnicalComplexityScore { get; set; }

        [StringLength(1000)]
        public string? EvaluationRemarks { get; set; }
    }
}
