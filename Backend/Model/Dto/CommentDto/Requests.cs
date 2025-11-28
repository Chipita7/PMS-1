using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.CommentDto.Requests
{
    public class CreateCommentDto
    {
        [Required]
        public int ProjectRequestId { get; set; }

        [Required]
        [StringLength(1000)]
        public string CommentText { get; set; } = string.Empty;
    }

    public class UpdateCommentDto
    {
        [Required]
        [StringLength(1000)]
        public string CommentText { get; set; } = string.Empty;
    }
}

