using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_PREFIX = "Config_";
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(AppDbContext context, IMemoryCache cache, ILogger<ConfigurationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<StatusConfig>> GetActiveStatusesAsync()
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}Statuses";

            if (!_cache.TryGetValue(cacheKey, out List<StatusConfig> statuses))
            {
                statuses = await _context.StatusConfigs
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SortOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, statuses, TimeSpan.FromHours(1));
            }

            return statuses;
        }

        public async Task<StatusConfig?> GetStatusByIdAsync(int id)
        {
            var statuses = await GetActiveStatusesAsync();
            return statuses.FirstOrDefault(s => s.Id == id);
        }

        public async Task<StatusConfig?> GetStatusByNameAsync(string name)
        {
            var statuses = await GetActiveStatusesAsync();
            return statuses.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<StatusConfig?> GetStatusByCodeAsync(string code)
        {
            var statuses = await GetActiveStatusesAsync();
            return statuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<PriorityConfig>> GetActivePrioritiesAsync()
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}Priorities";

            if (!_cache.TryGetValue(cacheKey, out List<PriorityConfig> priorities))
            {
                priorities = await _context.PriorityConfigs
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, priorities, TimeSpan.FromHours(1));
            }

            return priorities;
        }

        public async Task<bool> AddApprovalReadyStageAsync()
        {
            try
            {
                // Check if Approval Ready stage already exists
                var existingStage = await _context.WorkflowStageConfigs
                    .FirstOrDefaultAsync(w => w.Code == "APPROVAL_READY");

                if (existingStage != null)
                {
                    _logger.LogInformation("Approval Ready stage already exists with ID: {StageId}", existingStage.Id);
                    return true;
                }

                // Add the new stage
                var approvalReadyStage = new WorkflowStageConfig
                {
                    Name = "Approval Ready",
                    Code = "APPROVAL_READY",
                    Description = "All review tasks completed, ready for final approval",
                    SortOrder = 3, // Between Initial Evaluation (2) and Idea Refinement (4)
                    IsActive = true
                };

                _context.WorkflowStageConfigs.Add(approvalReadyStage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Added Approval Ready workflow stage with ID: {StageId}", approvalReadyStage.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error adding Approval Ready stage");
                return false;
            }
        }


        public async Task<PriorityConfig?> GetPriorityByIdAsync(int id)
        {
            var priorities = await GetActivePrioritiesAsync();
            return priorities.FirstOrDefault(p => p.Id == id);
        }

        public async Task<List<RequestTypeConfig>> GetActiveRequestTypesAsync()
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}RequestTypes";

            if (!_cache.TryGetValue(cacheKey, out List<RequestTypeConfig> requestTypes))
            {
                requestTypes = await _context.RequestTypeConfigs
                    .Where(rt => rt.IsActive)
                    .OrderBy(rt => rt.SortOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, requestTypes, TimeSpan.FromHours(1));
            }

            return requestTypes;
        }

        public async Task<List<StrategicAlignmentConfig>> GetActiveStrategicAlignmentsAsync()
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}StrategicAlignments";

            if (!_cache.TryGetValue(cacheKey, out List<StrategicAlignmentConfig> alignments))
            {
                alignments = await _context.StrategicAlignmentConfigs
                    .Where(sa => sa.IsActive)
                    .OrderBy(sa => sa.SortOrder)
                    .ToListAsync();

                _cache.Set(cacheKey, alignments, TimeSpan.FromHours(1));
            }

            return alignments;
        }

        public async Task<StrategicAlignmentConfig?> GetStrategicAlignmentByIdAsync(int id)
        {
            var alignments = await GetActiveStrategicAlignmentsAsync();
            return alignments.FirstOrDefault(sa => sa.Id == id);
        }

        public async Task<Dictionary<string, List<object>>> GetAllActiveConfigurationsAsync()
        {
            var result = new Dictionary<string, List<object>>();

            var statuses = await GetActiveStatusesAsync();
            var priorities = await GetActivePrioritiesAsync();
            var requestTypes = await GetActiveRequestTypesAsync();
            var strategicAlignments = await GetActiveStrategicAlignmentsAsync();

            // Add other config types as needed...

            result["Statuses"] = statuses.Cast<object>().ToList();
            result["Priorities"] = priorities.Cast<object>().ToList();
            result["RequestTypes"] = requestTypes.Cast<object>().ToList();
            result["StrategicAlignments"] = strategicAlignments.Cast<object>().ToList();

            return result;
        }

        public async Task RefreshCacheAsync()
        {
            var cacheKeys = new[] { "Statuses", "Priorities", "RequestTypes", "StrategicAlignments" };

            foreach (var key in cacheKeys)
            {
                _cache.Remove($"{CACHE_KEY_PREFIX}{key}");
            }

            // Force reload by calling get methods
            await GetActiveStatusesAsync();
            await GetActivePrioritiesAsync();
            await GetActiveRequestTypesAsync();
            await GetActiveStrategicAlignmentsAsync();
        }

        public async Task<WorkflowStageConfig?> GetWorkflowStageByCodeAsync(string code)
        {
            return await _context.WorkflowStageConfigs
                .FirstOrDefaultAsync(w => w.Code == code && w.IsActive);
        }
    }
}