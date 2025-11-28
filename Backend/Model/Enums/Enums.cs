// Models/Enums/ProjectRequestEnums.cs
namespace ProjectManagementSystem1.Models.Enums
{
    public enum FileType
    {
        Word,
        Excel,
        PPT,
        PDF,
        JPEG,
        JPG,
        PNG,
        Other
    }

    public enum AttachmentCategory
    {
        BusinessCase,
        CostEstimates,
        BusinessRequirementDocument,
        BusinessFeasibilityReport,
        TechnicalFeasibilityReport,
        UATTestReport,
        INSASecurityCertificate,
        SecurityClearance,
        RequestMemo,
        ResourceAssignmentMemo,
        Other
    }
}