namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses
{
    public class ProjectRequestDto
    {
        public int Id { get; set; }
        public string RequestID { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public string RequestDescription { get; set; } = string.Empty;
        public string? ReferenceNo { get; set; }

        // Config information (names for display)
        public string RequestType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }

        // Requestor info
        public string RequestedByName { get; set; } = string.Empty;
        public string BusinessDepartment { get; set; } = string.Empty;
        public string StrategicAlignment { get; set; } = string.Empty;

        // Dates
        public DateTime CreatedDate { get; set; }
        public int RequestDurationDays { get; set; }

        // Evaluation
        public decimal? TotalScore { get; set; }
        public string? AssignedTo { get; set; }
        public int? TimeToDeliveryDays { get; set; }
        public int? DaysUntilDelivery { get; set; }
        
        // ✅ FIXED: Head Reviewer information for frontend filtering
        public string? HeadReviewerId { get; set; }
        public string? HeadReviewerName { get; set; }
        public DateTime? HeadAssignedAt { get; set; }
    }
}