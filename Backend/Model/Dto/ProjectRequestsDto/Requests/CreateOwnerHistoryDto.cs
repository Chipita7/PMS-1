using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests
{
    public class CreateOwnerHistoryDto
    {
        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        public string OwnerRole { get; set; } = "Owner";

        public DateTime? StartDate { get; set; }
    }
}

