using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;

namespace ProjectManagementSystem1.Services.ApprovalAnalyticsService
{
    public interface IApprovalAnalyticsService
    {
        Task<ApprovalAnalyticsDto> GetApprovalAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<ApprovalTrendDto>> GetApprovalTrendsAsync(int days = 30);
        Task<ScoreAnalysisDto> GetScoreAnalysisAsync();
        Task<BulkApprovalResult> BulkApproveAsync(List<int> requestIds, string remarks, string currentUserId);
    }
}
