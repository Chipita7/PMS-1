using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class RequestIdSequence
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public int Counter { get; set; }
    }
}

