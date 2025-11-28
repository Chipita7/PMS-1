using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.UserManagementDto
{
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

    }
}
