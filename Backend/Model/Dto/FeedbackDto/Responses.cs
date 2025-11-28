using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.FeedbackDto.Responses
{
    public class FeedbackDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string AuthorID { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string FeedbackText { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? StakeholderRole { get; set; }
        public DateTime Timestamp { get; set; }
        public bool CanEdit { get; set; }
    }

    public class FeedbackStatsDto
    {
        public int RequestId { get; set; }
        public int TotalFeedback { get; set; }
        public double AverageRating { get; set; }
        public int RatingCounts { get; set; } // Number of ratings received
        public Dictionary<int, int> RatingDistribution { get; set; } = new(); // 1:5, 2:3, etc.
        public DateTime? LastFeedbackDate { get; set; }
    }

    public class FeedbackSummaryDto
    {
        public double OverallRating { get; set; }
        public int TotalResponses { get; set; }
        public List<RecentFeedbackDto> RecentFeedback { get; set; } = new();
    }

    public class RecentFeedbackDto
    {
        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string FeedbackText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}