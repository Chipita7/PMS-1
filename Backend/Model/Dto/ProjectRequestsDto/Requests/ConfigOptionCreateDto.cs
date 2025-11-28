namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests
{
    public class ConfigOptionCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public int SortOrder { get; set; }
    }
}
