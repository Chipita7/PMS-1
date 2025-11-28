using ProjectManagementSystem1.Model.Dto.FeedbackDto.Requests;
using ProjectManagementSystem1.Model.Dto.FeedbackDto.Responses;

namespace ProjectManagementSystem1.Services.FeedbackService
{
    public interface IFeedbackService
    {
        Task<FeedbackDto> SubmitFeedbackAsync(SubmitFeedbackDto submitDto, string currentUserId);
        Task<List<FeedbackDto>> GetFeedbackByRequestAsync(int requestId, string currentUserId);
        Task<FeedbackDto?> GetFeedbackByIdAsync(int feedbackId, string currentUserId);
        Task<FeedbackDto?> UpdateFeedbackAsync(int feedbackId, UpdateFeedbackDto updateDto, string currentUserId);
        Task<bool> DeleteFeedbackAsync(int feedbackId, string currentUserId);
        Task<FeedbackStatsDto> GetFeedbackStatsAsync(int requestId);
        Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(int requestId);
        Task<bool> HasUserGivenFeedbackAsync(int requestId, string userId);
        Task<bool> CanUserEditFeedbackAsync(int feedbackId, string currentUserId);
    }
}
