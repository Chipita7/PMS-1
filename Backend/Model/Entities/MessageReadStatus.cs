using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Model.Entities
{
    public class MessageReadStatus
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int MessageId { get; set; }

        [Required]
        public string UserId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadTime { get; set; }

        // Navigation (optional)
        [ForeignKey("MessageId")]
        public Message Message { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
