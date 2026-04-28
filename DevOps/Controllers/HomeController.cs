using DevOps.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DevOps.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        private const string ApiKeyHeaderName = "X-API-KEY";
        private const string ParseApiKeyHeaderName = "X-Parse-REST-API-Key";
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("DevOps")]
        public IActionResult DevOps([FromBody] RequestDevOps? request)
        {
            var hasApiKeyHeader = Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValue);
            if (!hasApiKeyHeader)
            {
                hasApiKeyHeader = Request.Headers.TryGetValue(ParseApiKeyHeaderName, out apiKeyHeaderValue);
            }

            if (!hasApiKeyHeader)
            {
                return Unauthorized("ERROR");
            }

            var expectedApiKey = _configuration["Security:ApiKey"];
            if (string.IsNullOrWhiteSpace(expectedApiKey) || apiKeyHeaderValue != expectedApiKey)
            {
                return Unauthorized("ERROR");
            }

            if (request is null || string.IsNullOrWhiteSpace(request.To))
            {
                return BadRequest("ERROR");
            }

            return Ok(new
            {
                message = $"Hello {request.To} your message will be sent"
            });
        }

        [AcceptVerbs("GET", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE", "CONNECT")]
        [Route("DevOps")]
        public IActionResult DevOpsInvalidMethod()
        {
            return Content("ERROR", "text/plain");
        }
    }
}