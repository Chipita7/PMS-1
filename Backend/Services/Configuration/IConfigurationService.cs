using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.Configuration
{
    public interface IConfigurationService
    {
        // Status Config
        Task<List<StatusConfig>> GetActiveStatusesAsync();
        Task<StatusConfig?> GetStatusByIdAsync(int id);
        Task<StatusConfig?> GetStatusByNameAsync(string name);
        Task<StatusConfig?> GetStatusByCodeAsync(string code);

        // Priority Config
        Task<List<PriorityConfig>> GetActivePrioritiesAsync();
        Task<PriorityConfig?> GetPriorityByIdAsync(int id);

        // Request Type Config
        Task<List<RequestTypeConfig>> GetActiveRequestTypesAsync();

        // Workflow Stage Config
        Task<WorkflowStageConfig?> GetWorkflowStageByCodeAsync(string code);

        // Strategic Alignment Config
        Task<List<StrategicAlignmentConfig>> GetActiveStrategicAlignmentsAsync();
        Task<StrategicAlignmentConfig?> GetStrategicAlignmentByIdAsync(int id);

        // All Configs
        Task<Dictionary<string, List<object>>> GetAllActiveConfigurationsAsync();
        Task<bool> AddApprovalReadyStageAsync();

        // Cache Management
        Task RefreshCacheAsync();
    }
}