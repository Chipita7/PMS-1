using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.CommentDto.Requests;
using ProjectManagementSystem1.Services.RequestCommentService;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestCommentsController : ControllerBase
    {
        private readonly IRequestCommentService _requestCommentService;
        private readonly ILogger<RequestCommentsController> _logger;

        public RequestCommentsController(IRequestCommentService requestCommentService, ILogger<RequestCommentsController> logger)
        {
            _requestCommentService = requestCommentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDto createDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var comment = await _requestCommentService.AddCommentAsync(createDto, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Comment added successfully",
                    data = comment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to request {RequestId}", createDto.ProjectRequestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetCommentsByRequest(int requestId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var comments = await _requestCommentService.GetCommentsByRequestAsync(requestId, currentUserId);

                return Ok(new
                {
                    success = true,
                    data = comments,
                    count = comments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("{commentId}")]
        public async Task<IActionResult> GetCommentById(int commentId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var comment = await _requestCommentService.GetCommentByIdAsync(commentId, currentUserId);

                if (comment == null)
                    return NotFound(new { success = false, error = "Comment not found" });

                return Ok(new
                {
                    success = true,
                    data = comment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {CommentId}", commentId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPut("{commentId}")]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentDto updateDto)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var comment = await _requestCommentService.UpdateCommentAsync(commentId, updateDto, currentUserId);

                if (comment == null)
                    return NotFound(new { success = false, error = "Comment not found" });

                return Ok(new
                {
                    success = true,
                    message = "Comment updated successfully",
                    data = comment
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
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var success = await _requestCommentService.DeleteCommentAsync(commentId, currentUserId);

                if (!success)
                    return NotFound(new { success = false, error = "Comment not found" });

                return Ok(new
                {
                    success = true,
                    message = "Comment deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("request/{requestId}/stats")]
        public async Task<IActionResult> GetCommentStats(int requestId)
        {
            try
            {
                var stats = await _requestCommentService.GetCommentStatsAsync(requestId);

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment stats for request {RequestId}", requestId);
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}
