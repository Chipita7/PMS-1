using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public class ProjectRequestReviewTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectRequestId { get; set; }

        [ForeignKey("ProjectRequestId")]
        public virtual ProjectRequest ProjectRequest { get; set; } = null!;

        // Task Assignment
        [Required]
        [StringLength(50)]
        public string AssigneeId { get; set; } = string.Empty; // AD User ID

        [Required]
        [StringLength(50)]
        public string AssigneeName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AssigneeRole { get; set; } = string.Empty; // "BusinessAnalyst", "InnovationChapter", "SME"

        [Required]
        [StringLength(500)]
        public string TaskDescription { get; set; } = string.Empty;

        // Assignment Details
        [Required]
        [StringLength(50)]
        public string AssignedById { get; set; } = string.Empty; // Head of PM

        [Required]
        [StringLength(50)]
        public string AssignedByName { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Deadline Management (3 business days)
        [Required]
        public DateTime DueDate { get; set; }

        // Status Tracking
        [Required]
        [StringLength(20)]
        public string TaskStatus { get; set; } = "Pending"; // Pending, InProgress, Completed, Overdue, Cancelled

        // Completion Tracking
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsOverdue => TaskStatus != "Completed" && DateTime.UtcNow > DueDate;

        [NotMapped]
        public int DaysUntilDue => (int)(DueDate - DateTime.UtcNow).TotalDays;

        [NotMapped]
        public int? DaysToComplete => CompletedAt.HasValue ? 
            (int)(CompletedAt.Value - AssignedAt).TotalDays : null;

        // Reminder Tracking
        public int ReminderCount { get; set; } = 0;
        public DateTime? LastReminderSent { get; set; }
    }
}