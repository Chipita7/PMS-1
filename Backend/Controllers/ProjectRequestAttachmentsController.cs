// Controllers/ProjectRequestAttachmentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.RequestAttachmentDto;
using ProjectManagementSystem1.Services.Attachments;
using ProjectManagementSystem1.Services.RequestAttachmentService;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/project-requests/attachments")]
    [Authorize]
    public class ProjectRequestAttachmentsController : ControllerBase
    {
        private readonly IProjectRequestAttachmentService _attachmentService;
        private readonly ILogger<ProjectRequestAttachmentsController> _logger;

        public ProjectRequestAttachmentsController(
            IProjectRequestAttachmentService attachmentService,
            ILogger<ProjectRequestAttachmentsController> logger)
        {
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment([FromForm] UploadProjectRequestAttachmentDto uploadDto)
        {
            try
            {
                var uploadedBy = User.Identity?.Name ?? "Unknown";
                var result = await _attachmentService.UploadAttachmentAsync(uploadDto, uploadedBy);
                return Ok(new { message = "File uploaded successfully", attachment = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for request {RequestId}", uploadDto.ProjectRequestId);
                return StatusCode(500, new { error = "Internal server error during file upload" });
            }
        }

        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetAttachments(int requestId)
        {
            try
            {
                var attachments = await _attachmentService.GetAttachmentsByRequestAsync(requestId);
                return Ok(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attachments for request {RequestId}", requestId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // ... other methods remain the same as previous version
        // [HttpGet("{attachmentId}"], [HttpPut], [HttpDelete], [HttpGet("download")], etc.
    }
}