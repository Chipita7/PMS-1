using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.EvaluationDto;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Evaluation;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationsController : ControllerBase
    {
        private readonly IEvaluationService _evaluationService;
        private readonly AppDbContext _context;
        private readonly ILogger<EvaluationsController> _logger;

        public EvaluationsController(IEvaluationService evaluationService, AppDbContext context, ILogger<EvaluationsController> logger)
        {
            _evaluationService = evaluationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("request/{requestId}")]
        public async Task<ActionResult> GetEvaluationsByRequest(int requestId)
        {
            try
            {
                _logger.LogInformation("Getting evaluations for request {RequestId}", requestId);

                // ✅ Verify request exists first
                var requestExists = await _context.ProjectRequests.AnyAsync(r => r.Id == requestId);
                if (!requestExists)
                {
                    return NotFound(new { message = $"Request {requestId} not found" });
                }

                var evaluations = await _evaluationService.GetEvaluationsByRequestAsync(requestId);

                var evaluationDtos = evaluations.Select(e => new EvaluationDetailDto
                {
                    Id = e.Id,
                    ReviewerId = e.ReviewerId,
                    ReviewerName = e.ReviewerName,
                    ReviewerRole = e.ReviewerRole.ToString(),
                    IsPrimaryEvaluation = e.IsPrimaryEvaluation,
                    StrategicAlignmentScore = e.StrategicAlignmentScore,
                    FeasibilityScore = e.FeasibilityScore,
                    BusinessValueScore = e.BusinessValueScore,
                    TechnicalComplexityScore = e.TechnicalComplexityScore,
                    OverallScore = e.StrategicAlignmentScore.HasValue &&
                                  e.FeasibilityScore.HasValue &&
                                  e.BusinessValueScore.HasValue &&
                                  e.TechnicalComplexityScore.HasValue
                        ? (e.StrategicAlignmentScore.Value + e.FeasibilityScore.Value +
                           e.BusinessValueScore.Value + e.TechnicalComplexityScore.Value) / 4.0
                        : null,
                    EvaluationRemarks = e.EvaluationRemarks,
                    SubmittedAt = e.SubmittedAt,
                    EvaluationStatus = e.EvaluationStatus,
                    AssignmentId = e.ProjectRequestAssignmentId
                }).ToList();

                var submittedCount = evaluationDtos.Count(e => e.EvaluationStatus == EvaluationStatus.Submitted);

                return Ok(new
                {
                    success = true,
                    requestId = requestId,
                    evaluations = evaluationDtos,
                    count = evaluationDtos.Count,
                    submittedCount = submittedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting evaluations for request {RequestId}", requestId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving evaluations",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetEvaluationById(int id)
        {
            try
            {
                _logger.LogInformation("Getting evaluation {EvaluationId}", id);

                var evaluation = await _evaluationService.GetEvaluationByIdAsync(id);

                if (evaluation == null)
                {
                    _logger.LogWarning("Evaluation {EvaluationId} not found", id);
                    return NotFound(new
                    {
                        success = false,
                        message = $"Evaluation with ID {id} not found"
                    });
                }

                var evaluationDto = new EvaluationDetailDto
                {
                    Id = evaluation.Id,
                    ReviewerId = evaluation.ReviewerId,
                    ReviewerName = evaluation.ReviewerName,
                    ReviewerRole = evaluation.ReviewerRole.ToString(),
                    IsPrimaryEvaluation = evaluation.IsPrimaryEvaluation,
                    StrategicAlignmentScore = evaluation.StrategicAlignmentScore,
                    FeasibilityScore = evaluation.FeasibilityScore,
                    BusinessValueScore = evaluation.BusinessValueScore,
                    TechnicalComplexityScore = evaluation.TechnicalComplexityScore,
                    OverallScore = evaluation.StrategicAlignmentScore.HasValue &&
                                  evaluation.FeasibilityScore.HasValue &&
                                  evaluation.BusinessValueScore.HasValue &&
                                  evaluation.TechnicalComplexityScore.HasValue
                        ? (evaluation.StrategicAlignmentScore.Value + evaluation.FeasibilityScore.Value +
                           evaluation.BusinessValueScore.Value + evaluation.TechnicalComplexityScore.Value) / 4.0
                        : null,
                    EvaluationRemarks = evaluation.EvaluationRemarks,
                    SubmittedAt = evaluation.SubmittedAt,
                    EvaluationStatus = evaluation.EvaluationStatus,
                    AssignmentId = evaluation.ProjectRequestAssignmentId
                };

                return Ok(new
                {
                    success = true,
                    evaluation = evaluationDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting evaluation {EvaluationId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving the evaluation",
                    error = ex.Message
                });
            }
        }

        [HttpPost("{id}/submit")]
        public async Task<ActionResult> SubmitEvaluation(int id, [FromBody] SubmitEvaluationDto evaluationDto)
        {
            try
            {
                _logger.LogInformation("Submitting evaluation {EvaluationId}", id);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for evaluation {EvaluationId}", id);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid evaluation data",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";

                var result = await _evaluationService.SubmitEvaluationAsync(id, evaluationDto, currentUserId);

                if (!result.Success)
                {
                    _logger.LogWarning("Evaluation submission failed for {EvaluationId}: {Message}", id, result.Message);
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }

                _logger.LogInformation("✅ Evaluation {EvaluationId} submitted successfully", id);
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    newTotalScore = result.NewTotalScore,
                    evaluationId = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error submitting evaluation {EvaluationId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while submitting the evaluation",
                    error = ex.Message
                });
            }
        }

        [HttpGet("my-evaluations")]
        public async Task<ActionResult> GetMyEvaluations()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not authenticated"
                    });
                }

                _logger.LogInformation("Getting evaluations for user {UserId}", currentUserId);

                var evaluations = await _context.ProjectRequestEvaluations
                    .Include(e => e.ProjectRequest)
                    .ThenInclude(pr => pr.StatusConfig)
                    .Include(e => e.ProjectRequestAssignment)
                    .Where(e => e.ReviewerId == currentUserId)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                var evaluationDtos = evaluations.Select(e => new
                {
                    id = e.Id,
                    requestId = e.ProjectRequestId,
                    requestTitle = e.ProjectRequest?.RequestTitle ?? "Unknown",
                    requestStatus = e.ProjectRequest?.StatusConfig?.Name ?? "Unknown",
                    evaluationStatus = e.EvaluationStatus.ToString(),
                    isPrimary = e.IsPrimaryEvaluation,
                    submittedAt = e.SubmittedAt,
                    overallScore = e.StrategicAlignmentScore.HasValue &&
                                  e.FeasibilityScore.HasValue &&
                                  e.BusinessValueScore.HasValue &&
                                  e.TechnicalComplexityScore.HasValue
                        ? (e.StrategicAlignmentScore.Value + e.FeasibilityScore.Value +
                           e.BusinessValueScore.Value + e.TechnicalComplexityScore.Value) / 4.0
                        : (double?)null,
                    canSubmit = e.EvaluationStatus == EvaluationStatus.Draft
                }).ToList();

                var draftCount = evaluationDtos.Count(e => e.evaluationStatus == "Draft");
                var submittedCount = evaluationDtos.Count(e => e.evaluationStatus == "Submitted");

                return Ok(new
                {
                    success = true,
                    evaluatorId = currentUserId,
                    evaluations = evaluationDtos,
                    draftCount = draftCount,
                    submittedCount = submittedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting evaluations for current user");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving evaluations",
                    error = ex.Message
                });
            }
        }
    }
}