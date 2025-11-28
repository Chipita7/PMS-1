namespace ProjectManagementSystem1.Model.Dto.WorkflowDto.Responses
{
        public class StatusHistoryDto
        {
            public int Id { get; set; }
            public string FromStatus { get; set; } = string.Empty;
            public string ToStatus { get; set; } = string.Empty;
            public string ChangedBy { get; set; } = string.Empty;
            public string ChangedByName { get; set; } = string.Empty;
            public DateTime ChangedAt { get; set; }
            public string? Remarks { get; set; }
            public int DurationInPreviousStatus { get; set; } // Days
        }

        public class WorkflowHistoryDto
        {
            public int Id { get; set; }
            public string WorkflowStage { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ChangedBy { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int? DurationDays { get; set; }
        }
    
}
