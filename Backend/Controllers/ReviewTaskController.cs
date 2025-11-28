using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ReviewTaskDto.Requests;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;
using ProjectManagementSystem1.Services.ReviewTasks;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewTasksController : ControllerBase
    {
        private readonly IReviewTaskService _reviewTaskService;
        private readonly ILogger<ReviewTasksController> _logger;
        private readonly AppDbContext _context;

        public ReviewTasksController(IReviewTaskService reviewTaskService, ILogger<ReviewTasksController> logger, AppDbContext context)
        {
            _reviewTaskService = reviewTaskService;
            _logger = logger;
            _context = context;
        }

        // ✅ OPTION C: Head of PM generates tasks from templates
        [HttpPost("request/{requestId}/generate-from-templates")]
        public async Task<IActionResult> GenerateTasksFromTemplates(
            int requestId,
            [FromBody] GenerateTasksFromTemplatesDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get from auth context
                var tasks = await _reviewTaskService.GenerateTasksFromTemplatesAsync(
                    requestId,
                    request.TaskCodes,
                    request.AssigneeOverrides,
                    request.CustomDueDays,
                    currentUserId);

                // In your controller, wrap responses like this:
                return Ok(new
                {
                    Success = true,
                    Message = $"Generated {tasks.Count} tasks successfully",
                    Data = tasks.Select(t => new {
                        Id = t.Id,
                        TaskDescription = t.TaskDescription,
                        AssigneeId = t.AssigneeId,
                        AssigneeRole = t.AssigneeRole,
                        DueDate = t.DueDate,
                        Status = t.TaskStatus
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tasks from templates for request {RequestId}", requestId);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("request/{requestId}/generate-for-reviewer")]
        public async Task<IActionResult> GenerateTasksForReviewerType(
    int requestId,
    [FromBody] GenerateTasksForReviewerTypeDto request)
        {
            try
            {
                var currentUserId = "current-user"; // Get from auth context
                var tasks = await _reviewTaskService.GenerateTasksForReviewerTypeAsync(
                    requestId,
                    request.ReviewerType,
                    request.AssigneeId,
                    request.CustomDueDays,
                    currentUserId);

                // ✅ RETURN CONSISTENT RESPONSE FORMAT
                return Ok(new
                {
                    Success = true,
                    Message = $"Generated {tasks.Count} tasks for {request.ReviewerType}",
                    Data = tasks.Select(t => new {
                        Id = t.Id,
                        TaskDescription = t.TaskDescription,
                        AssigneeId = t.AssigneeId,
                        AssigneeRole = t.AssigneeRole,
                        DueDate = t.DueDate,
                        Status = t.TaskStatus
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tasks for reviewer type {ReviewerType}", request.ReviewerType);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }
        
        // ✅ OPTION C: Manual task creation (already exists, keep it)
        [HttpPost("request/{requestId}/manual")]
        public async Task<IActionResult> CreateManualReviewTask(
    int requestId,
    [FromBody] CreateManualTaskDto request)
        {
            try
            {
                var currentUserId = "current-user"; // Get from auth context
                var task = await _reviewTaskService.CreateReviewTaskManuallyAsync(
                    requestId,
                    request.AssigneeId,
                    request.AssigneeName,
                    request.AssigneeRole,
                    request.TaskDescription,
                    request.DueDate,
                    currentUserId);

                // ✅ RETURN CONSISTENT RESPONSE FORMAT
                return Ok(new
                {
                    Success = true,
                    Message = "Manual task created successfully",
                    Data = new
                    {
                        Id = task.Id,
                        TaskDescription = task.TaskDescription,
                        AssigneeId = task.AssigneeId,
                        AssigneeRole = task.AssigneeRole,
                        DueDate = task.DueDate,
                        Status = task.TaskStatus
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual task for request {RequestId}", requestId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }
        // ✅ Get tasks for a request
        [HttpGet("request/{requestId}")]
        public async Task<IActionResult> GetTasksByRequest(int requestId)
        {
            try
            {
                var tasks = await _reviewTaskService.GetTasksByRequestAsync(requestId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for request {RequestId}", requestId);
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ✅ Get available task templates
        [HttpGet("templates")]
        public async Task<IActionResult> GetTaskTemplates()
        {
            try
            {
                var templates = await _context.ReviewTaskConfigs
                    .Where(rtc => rtc.IsActive)
                    .OrderBy(rtc => rtc.SortOrder)
                    .ToListAsync();

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task templates");
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Add these endpoints to your ReviewTasksController

        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var tasks = await _reviewTaskService.GetTasksByAssigneeAsync(currentUserId);

                return Ok(new
                {
                    Success = true,
                    AssigneeId = currentUserId,
                    Data = tasks.Select(t => new {
                        Id = t.Id,
                        TaskDescription = t.TaskDescription,
                        AssigneeRole = t.AssigneeRole,
                        DueDate = t.DueDate,
                        Status = t.TaskStatus,
                        IsOverdue = t.IsOverdue,
                        RequestId = t.ProjectRequestId,
                        RequestTitle = t.ProjectRequest?.RequestTitle ?? "Unknown"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for current user");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [HttpPut("{taskId}/complete")]
        public async Task<IActionResult> CompleteTask(int taskId, [FromBody] Model.Dto.ReviewTaskDto.Requests.CompleteTaskDto? request = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var completionRemarks = request?.CompletionRemarks;
                var result = await _reviewTaskService.CompleteTaskAsync(taskId, currentUserId, completionRemarks);

                if (!result)
                    return NotFound(new { Success = false, Message = $"Task {taskId} not found or already completed" });

                return Ok(new
                {
                    Success = true,
                    Message = "Task completed successfully",
                    CompletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId}", taskId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [HttpPut("{taskId}/reassign")]
        public async Task<IActionResult> ReassignTask(int taskId, [FromBody] ProjectManagementSystem1.Model.Dto.ReviewTaskDto.Requests.ReassignTaskDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var result = await _reviewTaskService.ReassignTaskAsync(taskId, request.NewAssigneeId, request.NewAssigneeName, currentUserId);

                if (!result)
                    return NotFound(new { Success = false, Message = $"Task {taskId} not found or you don't have permission to reassign it" });

                return Ok(new
                {
                    Success = true,
                    Message = "Task reassigned successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning task {TaskId}", taskId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "current-user";
                var result = await _reviewTaskService.UpdateTaskAsync(taskId, request.TaskDescription, request.DueDate, currentUserId);

                if (!result)
                    return NotFound(new { Success = false, Message = $"Task {taskId} not found" });

                return Ok(new
                {
                    Success = true,
                    Message = "Task updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", taskId);
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueTasks()
        {
            try
            {
                var tasks = await _reviewTaskService.GetOverdueTasksAsync();

                return Ok(new
                {
                    Success = true,
                    Count = tasks.Count,
                    Data = tasks.Select(t => new {
                        Id = t.Id,
                        TaskDescription = t.TaskDescription,
                        AssigneeId = t.AssigneeId,
                        AssigneeRole = t.AssigneeRole,
                        DueDate = t.DueDate,
                        Status = t.TaskStatus,
                        RequestId = t.ProjectRequestId,
                        RequestTitle = t.ProjectRequest?.RequestTitle ?? "Unknown",
                        DaysOverdue = (int)(DateTime.UtcNow - t.DueDate).TotalDays
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }
    }
}