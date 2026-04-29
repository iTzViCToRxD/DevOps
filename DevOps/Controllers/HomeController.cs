using DevOps.Dto;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DevOps.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : Controller
    {
        private const string ApiKeyHeaderName = "X-API-KEY";
        private const string ParseApiKeyHeaderName = "X-Parse-REST-API-Key";
        private const string JwtHeaderName = "X-JWT-KWY";
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _memoryCache;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
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

            if (!Request.Headers.TryGetValue(JwtHeaderName, out var jwtHeaderValue) || string.IsNullOrWhiteSpace(jwtHeaderValue))
            {
                _logger.LogWarning("Unauthorized request: JWT header is missing");
                return Unauthorized("ERROR");
            }

            if (!TryGetJwtJtiAndExpiry(jwtHeaderValue.ToString(), out var jti, out var expiresUtc))
            {
                _logger.LogWarning("Unauthorized request: JWT format is invalid");
                return Unauthorized("ERROR");
            }

            var replayCacheKey = $"jwt-jti:{jti}";
            if (_memoryCache.TryGetValue(replayCacheKey, out _))
            {
                _logger.LogWarning("Unauthorized request: JWT jti was already used");
                return Unauthorized("ERROR");
            }

            var ttl = expiresUtc.HasValue && expiresUtc.Value > DateTimeOffset.UtcNow
                ? expiresUtc.Value - DateTimeOffset.UtcNow
                : TimeSpan.FromMinutes(5);

            _memoryCache.Set(replayCacheKey, true, ttl);

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

        private static bool TryGetJwtJtiAndExpiry(string token, out string jti, out DateTimeOffset? expiresUtc)
        {
            jti = string.Empty;
            expiresUtc = null;

            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!TryBase64UrlDecode(parts[1], out var payloadJson))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(payloadJson);
                var root = document.RootElement;

                if (!root.TryGetProperty("jti", out var jtiElement) || string.IsNullOrWhiteSpace(jtiElement.GetString()))
                {
                    return false;
                }

                jti = jtiElement.GetString()!;

                if (root.TryGetProperty("exp", out var expElement) && expElement.ValueKind == JsonValueKind.Number && expElement.TryGetInt64(out var exp))
                {
                    expiresUtc = DateTimeOffset.FromUnixTimeSeconds(exp);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryBase64UrlDecode(string value, out string decoded)
        {
            decoded = string.Empty;
            var base64 = value.Replace('-', '+').Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
                case 0:
                    break;
                default:
                    return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                decoded = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
