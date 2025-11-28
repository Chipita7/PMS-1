using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.FeedbackDto.Requests;
using ProjectManagementSystem1.Services.FeedbackService;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger)
        {
            _feedbackService = feedbackService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackDto submitDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var feedback = await _feedbackService.SubmitFeedbackAsync(submitDto, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Feedback submitted successfully",
                    data = feedback
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback for request {RequestId}", submitDto.ProjectRequestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetFeedbackByRequest(int requestId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var feedback = await _feedbackService.GetFeedbackByRequestAsync(requestId, currentUserId);

                return Ok(new
                {
                    success = true,
                    data = feedback,
                    count = feedback.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("{feedbackId}")]
        public async Task<IActionResult> GetFeedbackById(int feedbackId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var feedback = await _feedbackService.GetFeedbackByIdAsync(feedbackId, currentUserId);

                if (feedback == null)
                    return NotFound(new { success = false, error = "Feedback not found" });

                return Ok(new
                {
                    success = true,
                    data = feedback
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback {FeedbackId}", feedbackId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPut("{feedbackId}")]
        public async Task<IActionResult> UpdateFeedback(int feedbackId, [FromBody] UpdateFeedbackDto updateDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var feedback = await _feedbackService.UpdateFeedbackAsync(feedbackId, updateDto, currentUserId);

                if (feedback == null)
                    return NotFound(new { success = false, error = "Feedback not found" });

                return Ok(new
                {
                    success = true,
                    message = "Feedback updated successfully",
                    data = feedback
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feedback {FeedbackId}", feedbackId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var success = await _feedbackService.DeleteFeedbackAsync(feedbackId, currentUserId);

                if (!success)
                    return NotFound(new { success = false, error = "Feedback not found" });

                return Ok(new
                {
                    success = true,
                    message = "Feedback deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feedback {FeedbackId}", feedbackId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}/stats")]
        public async Task<IActionResult> GetFeedbackStats(int requestId)
        {
            try
            {
                var stats = await _feedbackService.GetFeedbackStatsAsync(requestId);

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback stats for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}/summary")]
        public async Task<IActionResult> GetFeedbackSummary(int requestId)
        {
            try
            {
                var summary = await _feedbackService.GetFeedbackSummaryAsync(requestId);

                return Ok(new
                {
                    success = true,
                    data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback summary for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}/has-given")]
        public async Task<IActionResult> HasUserGivenFeedback(int requestId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var hasGiven = await _feedbackService.HasUserGivenFeedbackAsync(requestId, currentUserId);

                return Ok(new
                {
                    success = true,
                    data = new { hasGivenFeedback = hasGiven }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user gave feedback for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}