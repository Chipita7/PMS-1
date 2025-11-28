using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Services.Configuration;

namespace ProjectManagementSystem1.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly IDataMigrationService _migrationService;
        private readonly IConfigurationService _configService;

        public MigrationController(IDataMigrationService migrationService, IConfigurationService configService)
        {
            _migrationService = migrationService;
            _configService = configService;
        }

        [HttpPost("run-enum-migration")]
        public async Task<IActionResult> RunEnumMigration()
        {
            try
            {
                var result = await _migrationService.MigrateEnumDataToConfigAsync();

                if (result)
                {
                    await _configService.RefreshCacheAsync();
                    var report = await _migrationService.GetMigrationReportAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "Enum migration completed successfully",
                        report
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Enum migration failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("run-request-migration")]
        public async Task<IActionResult> RunRequestMigration()
        {
            try
            {
                var result = await _migrationService.MigrateExistingProjectRequestsAsync();

                if (result)
                {
                    var report = await _migrationService.GetMigrationReportAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "ProjectRequest migration completed successfully",
                        report
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "ProjectRequest migration failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("migration-report")]
        public async Task<IActionResult> GetMigrationReport()
        {
            var report = await _migrationService.GetMigrationReportAsync();
            return Ok(report);
        }

        [HttpGet("config-status")]
        public async Task<IActionResult> GetConfigStatus()
        {
            var configs = await _configService.GetAllActiveConfigurationsAsync();
            return Ok(configs);
        }
    }
}