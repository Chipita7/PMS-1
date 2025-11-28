using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests
{
    public class SubmitEvaluationRequest
    {
        [Range(1, 10)]
        public int FeasibilityScore { get; set; }

        [Range(1, 10)]
        public int BusinessValueScore { get; set; }

        [Range(1, 10)]
        public int TechnicalComplexityScore { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}

