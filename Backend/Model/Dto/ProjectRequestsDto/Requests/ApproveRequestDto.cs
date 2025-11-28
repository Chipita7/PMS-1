using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests
{
    public class ApproveRequestDto
    {
        [Required]
        public string Remarks { get; set; } = string.Empty;

        // Optional: allow different score thresholds per approval
        public decimal? MinimumScoreThreshold { get; set; }
    }

    public class ApprovalDashboardDto
    {
        public int TotalReadyForApproval { get; set; }
        public int HighPriorityCount { get; set; }
        public decimal AverageScore { get; set; }
        public Dictionary<string, int> ScoreDistribution { get; set; } = new();
        public List<ApprovalReadyRequestDto> Requests { get; set; } = new();
    }

    public class ApprovalReadyRequestDto
    {
        public int Id { get; set; }
        public string RequestID { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public decimal? TotalScore { get; set; }
        public string? Priority { get; set; }
        public string? PriorityColor { get; set; }
        public string? RequestType { get; set; }
        public DateTime CreatedDate { get; set; }
        public int DaysInReview { get; set; }
        public bool CanAutoApprove { get; set; }
    }

    public class ApprovalAnalyticsDto
    {
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public decimal ApprovalRate { get; set; }
        public double AverageTimeToApprove { get; set; }
        public Dictionary<string, int> ApprovalByScoreRange { get; set; } = new();
        public decimal HighScoreApprovalRate { get; set; }
        public decimal AverageScoreApproved { get; set; }
        public decimal AverageScoreRejected { get; set; }
    }

    public class ApprovalTrendDto
    {
        public DateTime Date { get; set; }
        public int ApprovedCount { get; set; }
        public decimal AverageScore { get; set; }
        public double AverageTimeToApprove { get; set; }
    }

    public class ScoreAnalysisDto
    {
        public int TotalRequestsWithScores { get; set; }
        public decimal AverageScoreAll { get; set; }
        public decimal AverageScoreApproved { get; set; }
        public decimal MinScoreApproved { get; set; }
        public decimal MaxScoreApproved { get; set; }
        public Dictionary<string, ScoreRangeStats> ScoreDistribution { get; set; } = new();
    }

    public class BulkApproveDto
    {
        [Required]
        public List<int> RequestIds { get; set; } = new();

        [Required]
        public string Remarks { get; set; } = string.Empty;

        //public bool OnlyHighScores { get; set; } = false;
        //public decimal? MinimumScore { get; set; } = 7.0m;
    }

    public class ScoreRangeStats
    {
        public int Count { get; set; }
        public decimal ApprovalRate { get; set; }
    }

    public class BulkApprovalResult
    {
        public int SuccessfulApprovals { get; set; }
        public int FailedApprovals { get; set; }
        public List<int> SuccessfulRequestIds { get; set; } = new();
        public List<FailedApproval> FailedRequests { get; set; } = new();
    }

    public class FailedApproval
    {
        public int RequestId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
    public class QuickApproveDto
    {
        [Required]
        public string Remarks { get; set; } = string.Empty;

        public bool AutoApproveHighScores { get; set; } = true;
        public decimal? HighScoreThreshold { get; set; } = 8.0m;
    }
}
