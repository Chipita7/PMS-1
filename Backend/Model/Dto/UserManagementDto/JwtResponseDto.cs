using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Model.Dto.UserManagementDto
{
    public class JwtResponseDto
    {
        public string Token { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsFirstLogin{ get; set; }
    }
}
