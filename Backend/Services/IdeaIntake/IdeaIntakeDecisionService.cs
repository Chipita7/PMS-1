using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.IdeaIntake
{
    public class IdeaIntakeDecisionService : IIdeaIntakeDecisionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IdeaIntakeDecisionService> _logger;

        public IdeaIntakeDecisionService(AppDbContext context, ILogger<IdeaIntakeDecisionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ShouldGoToIdeaRefinementAsync(ProjectRequest request)
        {
            try
            {
                var requestType = await _context.RequestTypeConfigs
                    .FirstOrDefaultAsync(rt => rt.Id == request.RequestTypeConfigId && rt.IsActive);

                if (requestType == null)
                {
                    return false;
                }

                if (requestType.RequiresIdeaRefinementAlways)
                {
                    return true;
                }

                var totalScore = request.TotalScore;
                if (totalScore.HasValue && (requestType.IdeaRefinementMinScore.HasValue || requestType.IdeaRefinementMaxScore.HasValue))
                {
                    var score = totalScore.Value;
                    var minOk = !requestType.IdeaRefinementMinScore.HasValue || score >= requestType.IdeaRefinementMinScore.Value;
                    var maxOk = !requestType.IdeaRefinementMaxScore.HasValue || score <= requestType.IdeaRefinementMaxScore.Value;

                    if (minOk && maxOk)
                    {
                        return true;
                    }
                }

                if (requestType.RequireIdeaRefinementIfHighRisk && request.RiskLevelConfigId.HasValue)
                {
                    var risk = await _context.ImpactUrgencyConfigs
                        .FirstOrDefaultAsync(r => r.Id == request.RiskLevelConfigId.Value);

                    if (risk != null && string.Equals(risk.Code, "HIGH", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deciding Idea Refinement for request {RequestId}", request.Id);
                return false;
            }
        }
    }
}
