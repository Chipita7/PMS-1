using ProjectManagementSystem1.Model;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services.ADService
{
    public interface IADService
    {
        Task<List<string>> GetAllUsersAsync();
        Task<ADUser?> GetUserAsync(string input);
        Task<ADUserDto?> GetUserByEmployeeIdAsync(string employeeId);
        //Task<string> CreateUserAsync(ADUser user);
    }
}