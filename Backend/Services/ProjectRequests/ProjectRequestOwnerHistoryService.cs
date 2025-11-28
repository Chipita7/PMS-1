using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Requests;
using ProjectManagementSystem1.Model.Dto.ProjectRequestsDto.Responses;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.Audit;

namespace ProjectManagementSystem1.Services.ProjectRequests
{
    public class ProjectRequestOwnerHistoryService : IProjectRequestOwnerHistoryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProjectRequestOwnerHistoryService> _logger;
        private readonly IProjectRequestAuditService _auditService;

        public ProjectRequestOwnerHistoryService(
            AppDbContext context,
            ILogger<ProjectRequestOwnerHistoryService> logger,
            IProjectRequestAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<List<ProjectRequestOwnerHistoryDto>> GetHistoryAsync(int requestId)
        {
            var histories = await _context.ProjectRequestOwnerHistories
                .Where(h => h.ProjectRequestId == requestId)
                .OrderByDescending(h => h.StartDate)
                .ToListAsync();

            return histories.Select(MapToDto).ToList();
        }

        public async Task<ProjectRequestOwnerHistoryDto> AddAsync(int requestId, CreateOwnerHistoryDto dto, string currentUserId)
        {
            var previousOwnerName = await GetLatestOwnerNameAsync(requestId) ?? "Unknown";
            await CloseActiveEntriesAsync(requestId);

            var history = new ProjectRequestOwnerHistory
            {
                ProjectRequestId = requestId,
                OwnerID = dto.OwnerId,
                OwnerName = dto.OwnerName,
                OwnerRole = dto.OwnerRole,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                Status = "Active"
            };

            _context.ProjectRequestOwnerHistories.Add(history);
            await _context.SaveChangesAsync();

            await _auditService.LogOwnerChangeAsync(requestId, currentUserId, previousOwnerName, dto.OwnerName);

            return MapToDto(history);
        }

        public async Task TrackOwnerChangeAsync(int requestId, string? newOwnerId, string? newOwnerName, string? ownerRole, string changedBy)
        {
            if (string.IsNullOrWhiteSpace(newOwnerId))
                return;

            try
            {
                var activeEntries = await _context.ProjectRequestOwnerHistories
                    .Where(h => h.ProjectRequestId == requestId && h.Status == "Active")
                    .OrderByDescending(h => h.StartDate)
                    .ToListAsync();

                // If the latest active entry already matches the owner, skip
                var latestEntry = activeEntries.FirstOrDefault();
                if (latestEntry != null && latestEntry.OwnerID == newOwnerId)
                    return;

                var previousOwnerName = latestEntry?.OwnerName ?? "Unknown";

                foreach (var entry in activeEntries)
                {
                    entry.EndDate = DateTime.UtcNow;
                    entry.Status = "Inactive";
                }

                var history = new ProjectRequestOwnerHistory
                {
                    ProjectRequestId = requestId,
                    OwnerID = newOwnerId,
                    OwnerName = newOwnerName ?? newOwnerId,
                    OwnerRole = ownerRole ?? "Owner",
                    StartDate = DateTime.UtcNow,
                    Status = "Active"
                };

                _context.ProjectRequestOwnerHistories.Add(history);
                await _context.SaveChangesAsync();

                await _auditService.LogOwnerChangeAsync(requestId, changedBy, previousOwnerName, history.OwnerName);
                _logger.LogInformation("Owner history updated for request {RequestId}: {Owner}", requestId, history.OwnerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking owner history for request {RequestId}", requestId);
            }
        }

        private async Task CloseActiveEntriesAsync(int requestId)
        {
            var activeEntries = await _context.ProjectRequestOwnerHistories
                .Where(h => h.ProjectRequestId == requestId && h.Status == "Active")
                .ToListAsync();

            foreach (var entry in activeEntries)
            {
                entry.EndDate = DateTime.UtcNow;
                entry.Status = "Inactive";
            }

            if (activeEntries.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task<string?> GetLatestOwnerNameAsync(int requestId)
        {
            return await _context.ProjectRequestOwnerHistories
                .Where(h => h.ProjectRequestId == requestId)
                .OrderByDescending(h => h.StartDate)
                .Select(h => h.OwnerName)
                .FirstOrDefaultAsync();
        }

        private static ProjectRequestOwnerHistoryDto MapToDto(ProjectRequestOwnerHistory history)
        {
            return new ProjectRequestOwnerHistoryDto
            {
                Id = history.Id,
                ProjectRequestId = history.ProjectRequestId,
                OwnerId = history.OwnerID,
                OwnerName = history.OwnerName,
                OwnerRole = history.OwnerRole,
                StartDate = history.StartDate,
                EndDate = history.EndDate,
                Status = history.Status
            };
        }
    }
}

