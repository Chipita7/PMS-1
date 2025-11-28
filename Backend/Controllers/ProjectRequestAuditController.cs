using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/project-requests/{requestId}/[controller]")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IProjectRequestAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(
            IProjectRequestAuditService auditService,
            ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditTrail(int requestId)
        {
            try
            {
                var auditTrail = await _auditService.GetAuditTrailAsync(requestId);
                return Ok(auditTrail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for request {RequestId}", requestId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetAuditSummary(int requestId)
        {
            try
            {
                var summary = await _auditService.GetAuditSummaryAsync(requestId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit summary for request {RequestId}", requestId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentActivity(int requestId, [FromQuery] int count = 50)
        {
            try
            {
                var recentActivity = await _auditService.GetRecentActivityAsync(requestId, count);
                return Ok(recentActivity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activity for request {RequestId}", requestId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}