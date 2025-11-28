namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses
{
    public class ProjectRequestOwnerHistoryDto
    {
        public int Id { get; set; }
        public int ProjectRequestId { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerRole { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public int? DurationDays => EndDate.HasValue ? (int)(EndDate.Value - StartDate).TotalDays : null;
    }
}

