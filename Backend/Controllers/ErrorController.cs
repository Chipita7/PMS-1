using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("error")]
    public class ErrorController : ControllerBase
    {
        [HttpGet]
        public IActionResult HandleError() 
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var traceId = HttpContext.TraceIdentifier;
            var message = feature?.Error?.Message ?? "An error occurred.";

            return Problem(detail: message, title: "Unhandled exception", statusCode: 500, instance: traceId);
        }

        [HttpGet("{code}")]
        public IActionResult HandleStatusCode(int code)
        {
            var traceId = HttpContext.TraceIdentifier;
            var payload = new
            {
                success = false,
                status = code,
                message = code switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not Found",
                    500 => "Internal Server Error",
                    _ => "Error"
                },
                correlationId = traceId
            };
            return StatusCode(code, payload);
        }
    }
}
