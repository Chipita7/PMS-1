using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Requests
{
    public class CreateProjectRequestDto
    {
        // Core Identification
        [Required]
        [StringLength(200)]
        public string RequestTitle { get; set; } = string.Empty;

        [Required]
        public string RequestDescription { get; set; } = string.Empty;

        [StringLength(15)]
        public string? ReferenceNo { get; set; }

        // Classification & Metadata
        [Required]
        public int RequestTypeConfigId { get; set; }

        [Required]
        public int RequestCategoryConfigId { get; set; }

        [Required]
        public int ServiceCategoryConfigId { get; set; }

        [Required]
        public int ProductCategoryConfigId { get; set; }

        [Required]
        public int PriorityConfigId { get; set; }

        [Required]
        public int BusinessImpactConfigId { get; set; }

        [Required]
        public int RequestUrgencyConfigId { get; set; }

        [Required]
        [StringLength(100)]
        public string StrategicAlignment { get; set; } = string.Empty;

        public int? StrategicAlignmentConfigId { get; set; }

        // Optional fields
        [Range(0, double.MaxValue)]
        public decimal? EstimatedCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedBenefit { get; set; }

        [Range(0, 60)] //In Months
        public int? BenefitCaptureDuration { get; set; }

        public DateTime? RequestedDeliveryDate { get; set; }

        public int? RiskLevelConfigId { get; set; }

        public int? ComplexityLevelConfigId { get; set; }

        // Add to CreateProjectRequestDto class
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

        [StringLength(50)]
        public string? RequestedByName { get; set; }

        [StringLength(50)]
        public string? BusinessSector { get; set; }

        [StringLength(50)]
        public string? BusinessDivision { get; set; }

        [StringLength(50)]
        public string? BusinessDepartment { get; set; }

        [StringLength(30)]
        public string? OrganizationName { get; set; }
    }
}