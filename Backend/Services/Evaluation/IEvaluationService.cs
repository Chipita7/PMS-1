using ProjectManagementSystem1.Model.Dto.EvaluationDto;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.Evaluation
{
    public interface IEvaluationService
    {
        Task<EvaluationResult> SubmitEvaluationAsync(int evaluationId, SubmitEvaluationDto evaluationDto, string currentUserId);
        Task<List<ProjectRequestEvaluation>> GetEvaluationsByRequestAsync(int requestId);
        Task<ProjectRequestEvaluation?> GetEvaluationByIdAsync(int evaluationId);
        Task<decimal?> UpdateEvaluationScoresAsync(int requestId, string currentUserId);
    }

    public class EvaluationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? NewTotalScore { get; set; }
        public List<string> Errors { get; set; } = new();

        public static EvaluationResult Successful(string message, decimal? newScore = null)
            => new() { Success = true, Message = message, NewTotalScore = newScore };

        public static EvaluationResult Failed(string error)
            => new() { Success = false, Errors = new List<string> { error } };
    }
}