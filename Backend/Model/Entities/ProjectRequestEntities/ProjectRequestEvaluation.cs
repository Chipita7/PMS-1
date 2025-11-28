using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public enum ReviewerRole
    {
        BusinessAnalyst,
        InnovationChapter,
        SubjectMatterExpert,
        HeadOfProductManagement,
        HeadOfEngineering,
        InformationSecurity
    }

    public enum EvaluationStatus
    {
        Draft,
        Submitted
    }

    public class ProjectRequestEvaluation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectRequestId { get; set; }

        [ForeignKey("ProjectRequestId")]
        public virtual ProjectRequest ProjectRequest { get; set; } = null!;

        public int? ProjectRequestAssignmentId { get; set; }

        [ForeignKey("ProjectRequestAssignmentId")]
        public virtual ProjectRequestAssignment? ProjectRequestAssignment { get; set; }

        // ✅ NEW: Track if this is the primary evaluation
        public bool IsPrimaryEvaluation { get; set; } = false;

   
        // Reviewer Information
        [Required]
        [StringLength(50)]
        public string ReviewerId { get; set; } = string.Empty; // AD User ID

        [Required]
        [StringLength(50)]
        public string ReviewerName { get; set; } = string.Empty; // Display name

        //[Required]
        //[StringLength(50)]
        public ReviewerRole ReviewerRole { get; set; } // "BusinessAnalyst", "InnovationChapter", "SME"

        // Evaluation Scores (1-5 scale)
        [Range(1, 10)]
        public int? StrategicAlignmentScore { get; set; }

        
        [Range(1, 10)]
        public int? FeasibilityScore { get; set; }

        [Range(1, 10)]
        public int? BusinessValueScore { get; set; }

        
        [Range(1, 10)]
        public int? TechnicalComplexityScore { get; set; } // 1=Very Complex, 5=Simple

        // Evaluation Details
        [StringLength(1000)]
        public string? EvaluationRemarks { get; set; }

        public DateTime? SubmittedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public double? OverallScore
        {
            get
            {
                // ✅ FIXED: Per requirements, only use 3 core scores (Feasibility, BusinessValue, TechnicalComplexity)
                // StrategicAlignmentScore is kept as optional field but NOT included in calculation
                if (!FeasibilityScore.HasValue || !BusinessValueScore.HasValue || !TechnicalComplexityScore.HasValue)
                    return null; // Return null if any of the 3 required scores is missing

                return (FeasibilityScore.Value + BusinessValueScore.Value + TechnicalComplexityScore.Value) / 3.0;
            }
        }

        // Status tracking
        [Required]
        [StringLength(20)]
        //public string EvaluationStatus { get; set; } = "Submitted"; // Draft, Submitted

        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.Draft;
        // Duration tracking

        [NotMapped]
        public int DaysToSubmit
        {
            get
            {
                if (!SubmittedAt.HasValue)
                    return 0; // Or whatever default you want for unsubmitted evaluations

                var timeSpan = SubmittedAt.Value - CreatedAt;
                return (int)Math.Ceiling(timeSpan.TotalDays);
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        //[NotMapped]
        //public double ReviewerTotalScore => (StrategicAlignmentScore + FeasibilityScore +
        //                               BusinessValueScore + TechnicalComplexityScore) / 4.0;
    }
}