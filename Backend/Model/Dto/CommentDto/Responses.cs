using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.CommentDto.Responses
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string AuthorID { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty; // Would come from AD
        public string CommentText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool CanEdit { get; set; } // Based on current user
    }

    public class CommentStatsDto
    {
        public int RequestId { get; set; }
        public int TotalComments { get; set; }
        public DateTime? LastCommentDate { get; set; }
        public int RecentCommentCount { get; set; } // Last 7 days
    }
}
