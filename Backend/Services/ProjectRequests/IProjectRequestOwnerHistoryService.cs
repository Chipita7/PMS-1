using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public interface IProjectRequestOwnerHistoryService
    {
        Task<List<ProjectRequestOwnerHistoryDto>> GetHistoryAsync(int requestId);
        Task<ProjectRequestOwnerHistoryDto> AddAsync(int requestId, CreateOwnerHistoryDto dto, string currentUserId);
        Task TrackOwnerChangeAsync(int requestId, string? newOwnerId, string? newOwnerName, string? ownerRole, string changedBy);
    }
}

