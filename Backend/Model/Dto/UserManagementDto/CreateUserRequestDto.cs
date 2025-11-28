namespace ProjectManagementSystem1.Model.Dto.UserManagementDto
{
    public class CreateUserRequestDto
    {
        public string EmployeeId { get; set; }
        public string Role { get; set; } // Admin chooses role

        // These are needed if AD fetch fails
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
