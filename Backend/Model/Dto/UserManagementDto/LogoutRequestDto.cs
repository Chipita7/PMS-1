using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.UserManagementDto
{
    public class LogoutRequestDto
    {
        [Required]
        public string refreshToken { get; set; }
    }
}
