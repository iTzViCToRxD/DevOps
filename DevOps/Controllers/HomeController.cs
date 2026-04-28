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
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("DevOps")]
        public IActionResult DevOps([FromBody] RequestDevOps? request)
        {
            _logger.LogInformation("Incoming request for /Home/DevOps");

            var hasApiKeyHeader = Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValue);
            if (!hasApiKeyHeader)
            {
                hasApiKeyHeader = Request.Headers.TryGetValue(ParseApiKeyHeaderName, out apiKeyHeaderValue);
            }

            if (!hasApiKeyHeader)
            {
                _logger.LogWarning("Unauthorized request: API key header is missing");
                return Unauthorized("ERROR");
            }

            var expectedApiKey = _configuration["Security:ApiKey"];
            if (string.IsNullOrWhiteSpace(expectedApiKey) || apiKeyHeaderValue != expectedApiKey)
            {
                _logger.LogWarning("Unauthorized request: API key validation failed");
                return Unauthorized("ERROR");
            }

            if (request is null || string.IsNullOrWhiteSpace(request.To))
            {
                _logger.LogWarning("Bad request on /Home/DevOps: payload is null or To is empty");
                return BadRequest("ERROR");
            }

            _logger.LogInformation("Request authorized successfully for recipient {Recipient}", request.To);

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
