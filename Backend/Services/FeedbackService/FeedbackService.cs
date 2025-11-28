using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.FeedbackDto.Requests;
using ProjectManagementSystem1.Model.Dto.FeedbackDto.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.FeedbackService
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(AppDbContext context, ILogger<FeedbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FeedbackDto> SubmitFeedbackAsync(SubmitFeedbackDto submitDto, string currentUserId)
        {
            try
            {
                _logger.LogInformation("⭐ Submitting feedback for request {RequestId} by user {UserId}",
                    submitDto.ProjectRequestId, currentUserId);

                // Verify request exists
                var request = await _context.ProjectRequests
                    .FirstOrDefaultAsync(r => r.Id == submitDto.ProjectRequestId);

                if (request == null)
                    throw new ArgumentException($"Request {submitDto.ProjectRequestId} not found");

                // Check if user already submitted feedback for this request
                var existingFeedback = await _context.ProjectRequestFeedbacks
                    .FirstOrDefaultAsync(f => f.ProjectRequestId == submitDto.ProjectRequestId &&
                                             f.AuthorID == currentUserId);

                if (existingFeedback != null)
                {
                    throw new InvalidOperationException("You have already submitted feedback for this request. You can update your existing feedback.");
                }

                var feedback = new ProjectRequestFeedback
                {
                    ProjectRequestId = submitDto.ProjectRequestId,
                    AuthorID = currentUserId,
                    FeedbackText = submitDto.FeedbackText,
                    Rating = submitDto.Rating,
                    Timestamp = DateTime.UtcNow
                };

                _context.ProjectRequestFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Feedback {FeedbackId} submitted successfully with rating {Rating}",
                    feedback.Id, submitDto.Rating);

                return await MapToFeedbackDto(feedback, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error submitting feedback for request {RequestId}", submitDto.ProjectRequestId);
                throw;
            }
        }

        public async Task<List<FeedbackDto>> GetFeedbackByRequestAsync(int requestId, string currentUserId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .Where(f => f.ProjectRequestId == requestId)
                    .OrderByDescending(f => f.Timestamp)
                    .ToListAsync();

                var dtos = new List<FeedbackDto>();
                foreach (var item in feedback)
                {
                    dtos.Add(await MapToFeedbackDto(item, currentUserId));
                }

                _logger.LogInformation("📋 Retrieved {Count} feedback items for request {RequestId}", dtos.Count, requestId);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving feedback for request {RequestId}", requestId);
                return new List<FeedbackDto>();
            }
        }

        public async Task<FeedbackDto?> GetFeedbackByIdAsync(int feedbackId, string currentUserId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);

                return feedback != null ? await MapToFeedbackDto(feedback, currentUserId) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving feedback {FeedbackId}", feedbackId);
                return null;
            }
        }

        public async Task<FeedbackDto?> UpdateFeedbackAsync(int feedbackId, UpdateFeedbackDto updateDto, string currentUserId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);

                if (feedback == null)
                    return null;

                // Security check - only author can edit
                if (feedback.AuthorID != currentUserId)
                {
                    _logger.LogWarning("🚫 User {UserId} attempted to edit feedback {FeedbackId} owned by {AuthorId}",
                        currentUserId, feedbackId, feedback.AuthorID);
                    throw new UnauthorizedAccessException("You can only edit your own feedback");
                }

                // Allow updates within 24 hours
                if (feedback.Timestamp < DateTime.UtcNow.AddHours(-24))
                {
                    throw new InvalidOperationException("Feedback can only be updated within 24 hours of submission");
                }

                feedback.FeedbackText = updateDto.FeedbackText;
                feedback.Rating = updateDto.Rating;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Feedback {FeedbackId} updated by user {UserId}", feedbackId, currentUserId);
                return await MapToFeedbackDto(feedback, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating feedback {FeedbackId}", feedbackId);
                throw;
            }
        }

        public async Task<bool> DeleteFeedbackAsync(int feedbackId, string currentUserId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);

                if (feedback == null)
                    return false;

                // Security check - only author can delete
                if (feedback.AuthorID != currentUserId)
                {
                    _logger.LogWarning("🚫 User {UserId} attempted to delete feedback {FeedbackId} owned by {AuthorId}",
                        currentUserId, feedbackId, feedback.AuthorID);
                    return false;
                }

                _context.ProjectRequestFeedbacks.Remove(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Feedback {FeedbackId} deleted by user {UserId}", feedbackId, currentUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting feedback {FeedbackId}", feedbackId);
                return false;
            }
        }

        public async Task<FeedbackStatsDto> GetFeedbackStatsAsync(int requestId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .Where(f => f.ProjectRequestId == requestId)
                    .ToListAsync();

                var stats = new FeedbackStatsDto
                {
                    RequestId = requestId,
                    TotalFeedback = feedback.Count,
                    AverageRating = feedback.Any() ? Math.Round(feedback.Average(f => f.Rating), 2) : 0,
                    RatingCounts = feedback.Count,
                    LastFeedbackDate = feedback.Any() ? feedback.Max(f => f.Timestamp) : null
                };

                // Calculate rating distribution (1-5 stars)
                for (int i = 1; i <= 5; i++)
                {
                    stats.RatingDistribution[i] = feedback.Count(f => f.Rating == i);
                }

                _logger.LogInformation("📊 Feedback stats for request {RequestId}: {Average} avg, {Count} total",
                    requestId, stats.AverageRating, stats.TotalFeedback);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting feedback stats for request {RequestId}", requestId);
                return new FeedbackStatsDto { RequestId = requestId };
            }
        }

        public async Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(int requestId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .Where(f => f.ProjectRequestId == requestId)
                    .OrderByDescending(f => f.Timestamp)
                    .Take(5) // Last 5 feedback items for summary
                    .ToListAsync();

                var allFeedback = await _context.ProjectRequestFeedbacks
                    .Where(f => f.ProjectRequestId == requestId)
                    .ToListAsync();

                var summary = new FeedbackSummaryDto
                {
                    OverallRating = allFeedback.Any() ? Math.Round(allFeedback.Average(f => f.Rating), 2) : 0,
                    TotalResponses = allFeedback.Count,
                    RecentFeedback = feedback.Select(f => new RecentFeedbackDto
                    {
                        AuthorName = $"User {f.AuthorID.Substring(0, 8)}", // Simple user display
                        Rating = f.Rating,
                        FeedbackText = f.FeedbackText.Length > 100 ?
                            f.FeedbackText.Substring(0, 100) + "..." : f.FeedbackText,
                        Timestamp = f.Timestamp
                    }).ToList()
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting feedback summary for request {RequestId}", requestId);
                return new FeedbackSummaryDto();
            }
        }

        public async Task<bool> HasUserGivenFeedbackAsync(int requestId, string userId)
        {
            try
            {
                return await _context.ProjectRequestFeedbacks
                    .AnyAsync(f => f.ProjectRequestId == requestId && f.AuthorID == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking if user gave feedback for request {RequestId}", requestId);
                return false;
            }
        }

        public async Task<bool> CanUserEditFeedbackAsync(int feedbackId, string currentUserId)
        {
            try
            {
                var feedback = await _context.ProjectRequestFeedbacks
                    .FirstOrDefaultAsync(f => f.Id == feedbackId);

                if (feedback == null) return false;

                // User can edit if they are the author AND feedback is less than 24 hours old
                return feedback.AuthorID == currentUserId &&
                       feedback.Timestamp >= DateTime.UtcNow.AddHours(-24);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking edit permission for feedback {FeedbackId}", feedbackId);
                return false;
            }
        }

        private async Task<FeedbackDto> MapToFeedbackDto(ProjectRequestFeedback feedback, string currentUserId)
        {
            // Simple user display without AD - show first 8 chars of user ID
            var authorName = $"User {feedback.AuthorID.Substring(0, 8)}";

            return new FeedbackDto
            {
                Id = feedback.Id,
                ProjectRequestId = feedback.ProjectRequestId,
                AuthorID = feedback.AuthorID,
                AuthorName = authorName,
                FeedbackText = feedback.FeedbackText,
                Rating = feedback.Rating,
                Timestamp = feedback.Timestamp,
                CanEdit = await CanUserEditFeedbackAsync(feedback.Id, currentUserId)
            };
        }
    }
}
