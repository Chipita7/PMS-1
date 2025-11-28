using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services.AuthService
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> LoginAsync(LoginRequestDto loginDto);
        Task<TokenResponseDto?> RefreshTokenAsync(string token);

        Task LogoutAsync(string userId);
    }
}
