namespace ProjectManagementSystem1.Models.DTOs.ProjectRequests.Responses
{
    public class ProjectRequestDetailDto
    {
        public int Id { get; set; }
        public string RequestID { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public string RequestDescription { get; set; } = string.Empty;
        public string? ReferenceNo { get; set; }

        // All config information
        public ConfigInfoDto RequestType { get; set; } = new();
        public ConfigInfoDto RequestCategory { get; set; } = new();
        public ConfigInfoDto ServiceCategory { get; set; } = new();
        public ConfigInfoDto ProductCategory { get; set; } = new();
        public ConfigInfoDto Priority { get; set; } = new();
        public ConfigInfoDto BusinessImpact { get; set; } = new();
        public ConfigInfoDto RequestUrgency { get; set; } = new();
        public ConfigInfoDto Status { get; set; } = new();
        public ConfigInfoDto? WorkflowStage { get; set; }
        public ConfigInfoDto? RiskLevel { get; set; }
        public ConfigInfoDto? ComplexityLevel { get; set; }
        public ConfigInfoDto? StrategicAlignmentInfo { get; set; }

        // Requestor details
        public string RequestedBy { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public string BusinessSector { get; set; } = string.Empty;
        public string BusinessDivision { get; set; } = string.Empty;
        public string BusinessDepartment { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string PrimaryContactEmail { get; set; } = string.Empty;
        public string? SecondaryContactEmail { get; set; }
        public string PrimaryContactPhone { get; set; } = string.Empty;
        public string? SecondaryContactPhone { get; set; }

        // Additional metadata
        public string StrategicAlignment { get; set; } = string.Empty;
        public decimal? EstimatedCost { get; set; }
        public decimal? EstimatedBenefit { get; set; }
        public int? BenefitCaptureDuration { get; set; }
        public DateTime? RequestedDeliveryDate { get; set; }

        // Evaluation
        public string? EvaluatorID { get; set; }
        public int? FeasibilityScore { get; set; }
        public int? BusinessValueScore { get; set; }
        public int? TechnicalComplexityScore { get; set; }
        public decimal? TotalScore { get; set; }
        public string? EvaluationRemarks { get; set; }

        // Workflow
        public string? AssignedTeam { get; set; }
        public string? AssignedTo { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }

        // Computed
        public int RequestDurationDays { get; set; }
        public int? TimeToDeliveryDays { get; set; }
        public int? DaysUntilDelivery { get; set; }
    }

    public class ConfigInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Color { get; set; }
    }
}