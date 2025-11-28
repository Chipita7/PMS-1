using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.WorkflowDto.Requests
{
    public class ApproveRequestDto
    {
        //[Required]
        //[StringLength(50)]
        //public string ApproverId { get; set; } = string.Empty; // Who is approving

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty; // Why it's being approved
    }

    public class RejectRequestDto
    {
        [Required]
        [StringLength(50)]
        public string RejectorId { get; set; } = string.Empty; // Who is rejecting

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty; // Why it's being rejected
    }
}
