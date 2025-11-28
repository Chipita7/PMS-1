using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests
{
    public class RejectRequestDto
    {
        public string Remarks { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rejection reason is required")]
        public RejectionReason Reason { get; set; }
    }
}
