using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Model.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Models.Enums; // ✅ Add this using

namespace ProjectManagementSystem1.Models.Entities.ProjectRequestEntities
{
    public enum RejectionReason
    {
        LowScore,           // Total score below threshold
        InsufficientInfo,   // Missing required information  
        NotAligned,         // Not aligned with strategy
        Duplicate,          // Duplicate request
        TechnicalFeasibility // Not technically feasible
    }
    public class ProjectRequest
    {
        [Key]
        public int Id { get; set; }

        // Core Identification Fields
        [Required]
        [StringLength(12)]
        public string RequestID { get; set; } = string.Empty; // ✅ Initialize

        [StringLength(15)]
        public string? ReferenceNo { get; set; }

        [Required]
        [StringLength(200)]
        public string RequestTitle { get; set; } = string.Empty; // ✅ Initialize

        [Required]
        public string RequestDescription { get; set; } = string.Empty; // ✅ Initialize

        // Requestor & Origin Details
        [Required]
        [StringLength(50)]
        public string RequestedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RequestedByName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BusinessSector { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BusinessDivision { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BusinessDepartment { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string OrganizationName { get; set; } = "Commercial Bank of Ethiopia";

        [Required]
        [EmailAddress]
        [StringLength(30)]
        public string PrimaryContactEmail { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(30)]
        public string? SecondaryContactEmail { get; set; }

        [Required]
        [StringLength(15)]
        public string PrimaryContactPhone { get; set; } = string.Empty;

        [StringLength(15)]
        public string? SecondaryContactPhone { get; set; }

        // Classification & Metadata - UPDATED TO USE CONFIG
        public RejectionReason? RejectionReason { get; set; }
        [StringLength(1000)]
        public string? RejectionRemarks { get; set; }

        [StringLength(50)]
        public string? RejectedBy { get; set; }

        public DateTime? RejectionDate { get; set; }

        [Required]
        public int RequestTypeConfigId { get; set; }

        [ForeignKey("RequestTypeConfigId")]
        public virtual RequestTypeConfig RequestTypeConfig { get; set; } = null!;

        [Required]
        public int RequestCategoryConfigId { get; set; }

        [ForeignKey("RequestCategoryConfigId")]
        public virtual RequestCategoryConfig RequestCategoryConfig { get; set; } = null!;

        [Required]
        public int ServiceCategoryConfigId { get; set; }

        [ForeignKey("ServiceCategoryConfigId")]
        public virtual ServiceCategoryConfig ServiceCategoryConfig { get; set; } = null!;

        [Required]
        public int ProductCategoryConfigId { get; set; }

        [ForeignKey("ProductCategoryConfigId")]
        public virtual ProductCategoryConfig ProductCategoryConfig { get; set; } = null!;

        [Required]
        public int PriorityConfigId { get; set; }

        [ForeignKey("PriorityConfigId")]
        public virtual PriorityConfig PriorityConfig { get; set; } = null!;

        [Required]
        public int BusinessImpactConfigId { get; set; }

        [ForeignKey("BusinessImpactConfigId")]
        public virtual ImpactUrgencyConfig BusinessImpactConfig { get; set; } = null!;

        [Required]
        public int RequestUrgencyConfigId { get; set; }

        [ForeignKey("RequestUrgencyConfigId")]
        public virtual ImpactUrgencyConfig RequestUrgencyConfig { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string StrategicAlignment { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal? EstimatedCost { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedBenefit { get; set; }
        public int? BenefitCaptureDuration { get; set; }
        public DateTime? RequestedDeliveryDate { get; set; }
        public int? RiskLevelConfigId { get; set; }

        [ForeignKey("RiskLevelConfigId")]
        public virtual ImpactUrgencyConfig? RiskLevelConfig { get; set; }

        public int? ComplexityLevelConfigId { get; set; }

        [ForeignKey("ComplexityLevelConfigId")]
        public virtual ImpactUrgencyConfig? ComplexityLevelConfig { get; set; }

        public int? StrategicAlignmentConfigId { get; set; }

        [ForeignKey("StrategicAlignmentConfigId")]
        public virtual StrategicAlignmentConfig? StrategicAlignmentConfig { get; set; }

        // Evaluation & Scoring
        [StringLength(50)]
        public string? EvaluatorID { get; set; }
        public int? FeasibilityScore { get; set; }
        public int? BusinessValueScore { get; set; }
        public int? TechnicalComplexityScore { get; set; }

        [Precision(18, 2)]
        public decimal? TotalScore { get; set; }
        public string? EvaluationRemarks { get; set; }

        // Workflow & Status Tracking
        [Required]
        public int StatusConfigId { get; set; }

        [ForeignKey("StatusConfigId")]
        public virtual StatusConfig StatusConfig { get; set; } = null!;

        public int? WorkflowStageConfigId { get; set; }

        [ForeignKey("WorkflowStageConfigId")]
        public virtual WorkflowStageConfig? WorkflowStageConfig { get; set; }

        [StringLength(100)]
        public string? AssignedTeam { get; set; }

        [StringLength(50)]
        public string? AssignedTo { get; set; }

        // Audit Fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        [StringLength(50)]
        public string LastUpdatedBy { get; set; } = string.Empty;

        public DateTime? ApprovalDate { get; set; }


        // ✅ FIXED: Navigation Properties - use virtual for lazy loading
        public virtual ICollection<ProjectRequestAttachment> Attachments { get; set; } = new List<ProjectRequestAttachment>();
        public virtual ICollection<ProjectRequestComment> Comments { get; set; } = new List<ProjectRequestComment>();
        public virtual ICollection<ProjectRequestFeedback> Feedbacks { get; set; } = new List<ProjectRequestFeedback>();
        public virtual ICollection<ProjectRequestWorkflowHistory> WorkflowHistories { get; set; } = new List<ProjectRequestWorkflowHistory>();
        public virtual ICollection<ProjectRequestStatusHistory> StatusHistories { get; set; } = new List<ProjectRequestStatusHistory>();
        public virtual ICollection<ProjectRequestOwnerHistory> OwnerHistories { get; set; } = new List<ProjectRequestOwnerHistory>();
        // Add to ProjectRequest class
        public virtual ICollection<ProjectRequestAssignment> Assignments { get; set; } = new List<ProjectRequestAssignment>();
        // Computed property
        public virtual ICollection<ProjectRequestReviewTask> ReviewTasks { get; set; } = new List<ProjectRequestReviewTask>();

        [NotMapped]
        public int RequestDurationDays
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedDate;
                var days = (int)Math.Ceiling(timeSpan.TotalDays);
                return Math.Max(1, days);
            }
        }

        [NotMapped]
        public int? TimeToDeliveryDays
        {
            get
            {
                if (!RequestedDeliveryDate.HasValue) return null;

                try
                {
                    var timeSpan = RequestedDeliveryDate.Value - CreatedDate;
                    return Math.Max(1, (int)Math.Ceiling(timeSpan.TotalDays));
                }
                catch
                {
                    return null;
                }
            }
        }

        [NotMapped]
        public int? DaysUntilDelivery
        {
            get
            {
                if (!RequestedDeliveryDate.HasValue) return null;

                try
                {
                    var timeSpan = RequestedDeliveryDate.Value - DateTime.UtcNow;
                    var days = (int)Math.Ceiling(timeSpan.TotalDays);
                    return days < 0 ? 0 : days;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}