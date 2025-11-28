using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Model.Entities
{
    public class UserAccessToken
    {
        public int Id { get; set; }

        [Required]
        public string AccessToken { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        [Required]
        public string UserId { get; set; } // Identity UserId (string)

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
