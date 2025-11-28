using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.ApprovalAnalyticsService;
using ProjectManagementSystem1.Services.ProjectRequests;

namespace ProjectManagementSystem1.Services.Analytics
{

    public class ApprovalAnalyticsService : IApprovalAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly IProjectRequestService _projectRequestService;
        private readonly ILogger<ApprovalAnalyticsService> _logger;

        public ApprovalAnalyticsService(AppDbContext context, IProjectRequestService projectRequestService, ILogger<ApprovalAnalyticsService> logger)
        {
            _context = context;
            _projectRequestService = projectRequestService;
            _logger = logger;
        }

        public async Task<ApprovalAnalyticsDto> GetApprovalAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var approvedStatus = await _context.StatusConfigs.FirstOrDefaultAsync(s => s.Code == "APPROVED");
                var rejectedStatus = await _context.StatusConfigs.FirstOrDefaultAsync(s => s.Code == "REJECTED");

                if (approvedStatus == null || rejectedStatus == null)
                {
                    _logger.LogWarning("Approved or Rejected status not found");
                    return new ApprovalAnalyticsDto();
                }

                var query = _context.ProjectRequests.AsQueryable();

                // Apply date filter if provided
                if (startDate.HasValue)
                    query = query.Where(r => r.CreatedDate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(r => r.CreatedDate <= endDate.Value);

                var requests = await query.ToListAsync();

                var approvedRequests = requests.Where(r => r.StatusConfigId == approvedStatus.Id && r.ApprovalDate.HasValue).ToList();
                var rejectedRequests = requests.Where(r => r.StatusConfigId == rejectedStatus.Id).ToList();
                var totalDecisions = approvedRequests.Count + rejectedRequests.Count;

                var analytics = new ApprovalAnalyticsDto
                {
                    TotalApproved = approvedRequests.Count,
                    TotalRejected = rejectedRequests.Count,
                    ApprovalRate = totalDecisions > 0 ? (decimal)approvedRequests.Count / totalDecisions * 100 : 0,
                    AverageTimeToApprove = approvedRequests.Any() ?
                        approvedRequests.Average(r => (r.ApprovalDate.Value - r.CreatedDate).TotalDays) : 0,
                    ApprovalByScoreRange = approvedRequests
                        .Where(r => r.TotalScore.HasValue)
                        .GroupBy(r => GetScoreRange(r.TotalScore.Value))
                        .ToDictionary(g => g.Key, g => g.Count()),
                    HighScoreApprovalRate = GetHighScoreApprovalRate(approvedRequests, rejectedRequests), // ✅ FIXED
                    AverageScoreApproved = approvedRequests.Where(r => r.TotalScore.HasValue).Average(r => r.TotalScore.Value),
                    AverageScoreRejected = rejectedRequests.Where(r => r.TotalScore.HasValue).Average(r => r.TotalScore.Value)
                };

                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval analytics");
                return new ApprovalAnalyticsDto();
            }
        }

        public async Task<List<ApprovalTrendDto>> GetApprovalTrendsAsync(int days = 30)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var approvedStatus = await _context.StatusConfigs.FirstOrDefaultAsync(s => s.Code == "APPROVED");

                if (approvedStatus == null) return new List<ApprovalTrendDto>();

                var trends = await _context.ProjectRequests
                    .Where(r => r.StatusConfigId == approvedStatus.Id && r.ApprovalDate >= startDate)
                    .GroupBy(r => r.ApprovalDate.Value.Date)
                    .Select(g => new ApprovalTrendDto
                    {
                        Date = g.Key,
                        ApprovedCount = g.Count(),
                        AverageScore = g.Average(r => r.TotalScore ?? 0),
                        AverageTimeToApprove = g.Average(r => (r.ApprovalDate.Value - r.CreatedDate).TotalDays)
                    })
                    .OrderBy(t => t.Date)
                    .ToListAsync();

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval trends");
                return new List<ApprovalTrendDto>();
            }
        }

        public async Task<ScoreAnalysisDto> GetScoreAnalysisAsync()
        {
            try
            {
                var requestsWithScores = await _context.ProjectRequests
                    .Where(r => r.TotalScore.HasValue)
                    .ToListAsync();

                var approvedStatus = await _context.StatusConfigs.FirstOrDefaultAsync(s => s.Code == "APPROVED");
                var approvedRequests = requestsWithScores.Where(r => r.StatusConfigId == approvedStatus?.Id).ToList();

                return new ScoreAnalysisDto
                {
                    TotalRequestsWithScores = requestsWithScores.Count,
                    AverageScoreAll = requestsWithScores.Average(r => r.TotalScore.Value),
                    AverageScoreApproved = approvedRequests.Any() ? approvedRequests.Average(r => r.TotalScore.Value) : 0,
                    MinScoreApproved = approvedRequests.Any() ? approvedRequests.Min(r => r.TotalScore.Value) : 0,
                    MaxScoreApproved = approvedRequests.Any() ? approvedRequests.Max(r => r.TotalScore.Value) : 0,
                    ScoreDistribution = requestsWithScores
                        .GroupBy(r => GetScoreRange(r.TotalScore.Value))
                        .ToDictionary(g => g.Key, g => new ScoreRangeStats
                        {
                            Count = g.Count(),
                            ApprovalRate = approvedRequests.Any(r => GetScoreRange(r.TotalScore.Value) == g.Key) ?
                                (decimal)g.Count(r => r.StatusConfigId == approvedStatus?.Id) / g.Count() * 100 : 0
                        })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting score analysis");
                return new ScoreAnalysisDto();
            }
        }

        public async Task<BulkApprovalResult> BulkApproveAsync(List<int> requestIds, string remarks, string currentUserId)
        {
            var result = new BulkApprovalResult();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var requestId in requestIds)
                {
                    try
                    {
                        var approved = await _projectRequestService.ApproveAsync(requestId, remarks, currentUserId);

                        if (approved)
                        {
                            result.SuccessfulApprovals++;
                            result.SuccessfulRequestIds.Add(requestId);
                        }
                        else
                        {
                            result.FailedApprovals++;
                            result.FailedRequests.Add(new FailedApproval
                            {
                                RequestId = requestId,
                                Reason = "Approval failed - may not meet criteria"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedApprovals++;
                        result.FailedRequests.Add(new FailedApproval
                        {
                            RequestId = requestId,
                            Reason = $"Error: {ex.Message}"
                        });
                        _logger.LogError(ex, "Error approving request {RequestId} in bulk operation", requestId);
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Bulk approval completed: {SuccessCount} successful, {FailedCount} failed",
                    result.SuccessfulApprovals, result.FailedApprovals);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Bulk approval transaction failed");
                result.FailedApprovals = requestIds.Count;
                result.SuccessfulApprovals = 0;
                return result;
            }
        }

        private string GetScoreRange(decimal score)
        {
            return score switch
            {
                < 4 => "0-4",
                < 6 => "4-6",
                < 8 => "6-8",
                < 9 => "8-9",
                _ => "9-10"
            };
        }

        private decimal GetHighScoreApprovalRate(List<ProjectRequest> approved, List<ProjectRequest> rejected)
        {
            var highScoreApproved = approved.Count(r => r.TotalScore >= 8.0m);
            var highScoreRejected = rejected.Count(r => r.TotalScore >= 8.0m);
            var totalHighScore = highScoreApproved + highScoreRejected;

            return totalHighScore > 0 ? (decimal)highScoreApproved / totalHighScore * 100 : 0;
        }
    }
}