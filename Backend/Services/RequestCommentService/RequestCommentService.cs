using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.CommentDto.Requests;
using ProjectManagementSystem1.Model.Dto.CommentDto.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Audit;

namespace ProjectManagementSystem1.Services.RequestCommentService
{
    public class RequestCommentService : IRequestCommentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RequestCommentService> _logger;
        private readonly IProjectRequestAuditService _auditService;

        public RequestCommentService(AppDbContext context, ILogger<RequestCommentService> logger, IProjectRequestAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<CommentDto> AddCommentAsync(CreateCommentDto createDto, string currentUserId)
        {
            try
            {
                _logger.LogInformation("💬 Adding comment to request {RequestId} by user {UserId}",
                    createDto.ProjectRequestId, currentUserId);

                // Verify request exists
                var request = await _context.ProjectRequests
                    .FirstOrDefaultAsync(r => r.Id == createDto.ProjectRequestId);

                if (request == null)
                    throw new ArgumentException($"Request {createDto.ProjectRequestId} not found");

                var comment = new ProjectRequestComment
                {
                    ProjectRequestId = createDto.ProjectRequestId,
                    AuthorID = currentUserId,
                    CommentText = createDto.CommentText,
                    Timestamp = DateTime.UtcNow
                };

                _context.ProjectRequestComments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Comment {CommentId} added successfully", comment.Id);

                 await _auditService.LogEntityActionAsync(createDto.ProjectRequestId, currentUserId, "Comment", comment.Id,
                    "CommentAdded", $"Comment added: {comment.CommentText}");

                return await MapToCommentDto(comment, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding comment to request {RequestId}", createDto.ProjectRequestId);
                throw;
            }
        }

        public async Task<List<CommentDto>> GetCommentsByRequestAsync(int requestId, string currentUserId)
        {
            try
            {
                var comments = await _context.ProjectRequestComments
                    .Where(c => c.ProjectRequestId == requestId)
                    .OrderByDescending(c => c.Timestamp)
                    .ToListAsync();

                var dtos = new List<CommentDto>();
                foreach (var comment in comments)
                {
                    dtos.Add(await MapToCommentDto(comment, currentUserId));
                }

                _logger.LogInformation("📋 Retrieved {Count} comments for request {RequestId}", dtos.Count, requestId);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving comments for request {RequestId}", requestId);
                return new List<CommentDto>();
            }
        }

        public async Task<CommentDto?> GetCommentByIdAsync(int commentId, string currentUserId)
        {
            try
            {
                var comment = await _context.ProjectRequestComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                return comment != null ? await MapToCommentDto(comment, currentUserId) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving comment {CommentId}", commentId);
                return null;
            }
        }

        public async Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentDto updateDto, string currentUserId)
        {
            try
            {
                var comment = await _context.ProjectRequestComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                    return null;

                // Security check - only author can edit
                if (comment.AuthorID != currentUserId)
                {
                    _logger.LogWarning("🚫 User {UserId} attempted to edit comment {CommentId} owned by {AuthorId}",
                        currentUserId, commentId, comment.AuthorID);
                    throw new UnauthorizedAccessException("You can only edit your own comments");
                }

                // Prevent editing comments older than 1 hour
                if (comment.Timestamp < DateTime.UtcNow.AddHours(-1))
                {
                    throw new InvalidOperationException("Comments can only be edited within 1 hour of posting");
                }

                comment.CommentText = updateDto.CommentText;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Comment {CommentId} updated by user {UserId}", commentId, currentUserId);

                await _auditService.LogEntityActionAsync(comment.ProjectRequestId, currentUserId, "Comment", comment.Id,
                    "CommentUpdated", $"Comment updated: {comment.CommentText}");
                return await MapToCommentDto(comment, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating comment {CommentId}", commentId);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string currentUserId)
        {
            try
            {
                var comment = await _context.ProjectRequestComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                    return false;

                // Security check - only author can delete
                if (comment.AuthorID != currentUserId)
                {
                    _logger.LogWarning("🚫 User {UserId} attempted to delete comment {CommentId} owned by {AuthorId}",
                        currentUserId, commentId, comment.AuthorID);
                    return false;
                }

                _context.ProjectRequestComments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Comment {CommentId} deleted by user {UserId}", commentId, currentUserId);

                await _auditService.LogEntityActionAsync(comment.ProjectRequestId, currentUserId, "Comment", commentId,
                    "CommentDeleted", "Comment deleted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<CommentStatsDto> GetCommentStatsAsync(int requestId)
        {
            try
            {
                var comments = await _context.ProjectRequestComments
                    .Where(c => c.ProjectRequestId == requestId)
                    .ToListAsync();

                var stats = new CommentStatsDto
                {
                    RequestId = requestId,
                    TotalComments = comments.Count,
                    LastCommentDate = comments.Any() ? comments.Max(c => c.Timestamp) : null,
                    RecentCommentCount = comments.Count(c => c.Timestamp >= DateTime.UtcNow.AddDays(-7))
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting comment stats for request {RequestId}", requestId);
                return new CommentStatsDto { RequestId = requestId };
            }
        }

        public async Task<bool> CanUserEditCommentAsync(int commentId, string currentUserId)
        {
            try
            {
                var comment = await _context.ProjectRequestComments
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null) return false;

                // User can edit if they are the author AND comment is less than 1 hour old
                return comment.AuthorID == currentUserId &&
                       comment.Timestamp >= DateTime.UtcNow.AddHours(-1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking edit permission for comment {CommentId}", commentId);
                return false;
            }
        }

        private async Task<CommentDto> MapToCommentDto(ProjectRequestComment comment, string currentUserId)
        {
            // In real scenario, you'd get author name from AD
            var authorName = await GetUserNameFromAdAsync(comment.AuthorID);

            return new CommentDto
            {
                Id = comment.Id,
                ProjectRequestId = comment.ProjectRequestId,
                AuthorID = comment.AuthorID,
                AuthorName = authorName ?? comment.AuthorID, // Fallback to ID if name not found
                CommentText = comment.CommentText,
                Timestamp = comment.Timestamp,
                CanEdit = await CanUserEditCommentAsync(comment.Id, currentUserId)
            };
        }

        private async Task<string?> GetUserNameFromAdAsync(string userId)
        {
            // TODO: Integrate with AD to get user display name
            // For now, return null to use userId as fallback
            return await Task.FromResult<string?>(null);
        }
    }
}
