namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses
{
    public class ConfigOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public int RequestDurationDays { get; set; }
        public int? TimeToDeliveryDays { get; set; }
        public int? DaysUntilDelivery { get; set; }
    }
}