using ProjectManagementSystem1.Model.Dto.CommentDto.Requests;
using ProjectManagementSystem1.Model.Dto.CommentDto.Responses;

namespace ProjectManagementSystem1.Services.RequestCommentService
{
    public interface IRequestCommentService
    {
        Task<CommentDto> AddCommentAsync(CreateCommentDto createDto, string currentUserId);
        Task<List<CommentDto>> GetCommentsByRequestAsync(int requestId, string currentUserId);
        Task<CommentDto?> GetCommentByIdAsync(int commentId, string currentUserId);
        Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentDto updateDto, string currentUserId);
        Task<bool> DeleteCommentAsync(int commentId, string currentUserId);
        Task<CommentStatsDto> GetCommentStatsAsync(int requestId);
        Task<bool> CanUserEditCommentAsync(int commentId, string currentUserId);
    }

}
