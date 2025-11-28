using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Models.Dto.Workflow.Requests
{
    public class UpdateStatusRequestDto
    {
        [Required]
        public int StatusConfigId { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    //public class AssignRequestDto
    //{
    //    [Required]
    //    [StringLength(100)]
    //    public string AssignedTeam { get; set; } = string.Empty;

    //    [Required]
    //    [StringLength(50)]
    //    public string AssignedTo { get; set; } = string.Empty;

    //    [StringLength(100)]
    //    public string AssigneeRole { get; set; } = "Team Member"; // Default role

    //    public int? WorkflowStageConfigId { get; set; }
    //}

        public class AssignmentResult
        {
            public bool IsSuccessful { get; set; }  // ✅ Property name
            public string? Message { get; set; }
            public string? PrimaryAssigneeId { get; set; }
            public List<string> Errors { get; set; } = new();

            public static AssignmentResult CreateSuccess(string? primaryAssigneeId)  // ✅ Method name
                => new()
                {
                    IsSuccessful = true,
                    PrimaryAssigneeId = primaryAssigneeId,
                    Message = "Assignment successful"
                };

            public static AssignmentResult CreateFailure(string error)  // ✅ Method name
                => new()
                {
                    IsSuccessful = false,
                    Errors = new List<string> { error }
                };

            public static AssignmentResult CreateFailure(List<string> errors)  // ✅ Overload
                => new()
                {
                    IsSuccessful = false,
                    Errors = errors
                };
        }
    

    public class SubmitEvaluationDto
    {
        internal int StrategicAlignmentScore;

        [Required]
        [Range(1, 10)]
        public int FeasibilityScore { get; set; }

        [Required]
        [Range(1, 10)]
        public int BusinessValueScore { get; set; }

        [Required]
        [Range(1, 10)]
        public int TechnicalComplexityScore { get; set; }

        [Required]
        [StringLength(1000)]
        public string EvaluationRemarks { get; set; } = string.Empty;

        [StringLength(50)]
        public string? EvaluatorId { get; set; } // Optional - can use current user
    }
}