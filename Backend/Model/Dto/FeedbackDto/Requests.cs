using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.FeedbackDto.Requests
{
    public class SubmitFeedbackDto
    {
        [Required]
        public int ProjectRequestId { get; set; }

        [Required]
        [StringLength(2000)]
        public string FeedbackText { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1-5 stars

        public string? StakeholderRole { get; set; } // Optional: "End User", "Manager", "Customer"
    }

    public class UpdateFeedbackDto
    {
        [Required]
        [StringLength(2000)]
        public string FeedbackText { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }
}

