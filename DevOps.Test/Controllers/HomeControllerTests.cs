using DevOps.Controllers;
using DevOps.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using Xunit;

namespace DevOps.Test.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void DevOps_ReturnsUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        var controller = CreateController(configApiKey: "expected-key");

        var result = controller.DevOps(new RequestDevOps { To = "Juan" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("ERROR", unauthorized.Value);
    }

    [Fact]
    public void DevOps_ReturnsUnauthorized_WhenApiKeyIsInvalid()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-API-KEY",
            headerValue: "invalid-key",
            jwtHeaderValue: CreateUnsignedJwt("tx-invalid-api-key"));

        var result = controller.DevOps(new RequestDevOps { To = "Juan" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("ERROR", unauthorized.Value);
    }

    [Fact]
    public void DevOps_ReturnsBadRequest_WhenRequestIsNull()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-API-KEY",
            headerValue: "expected-key",
            jwtHeaderValue: CreateUnsignedJwt("tx-null-request"));

        var result = controller.DevOps(null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ERROR", badRequest.Value);
    }

    [Fact]
    public void DevOps_ReturnsBadRequest_WhenToIsEmpty()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-API-KEY",
            headerValue: "expected-key",
            jwtHeaderValue: CreateUnsignedJwt("tx-empty-to"));

        var result = controller.DevOps(new RequestDevOps { To = " " });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ERROR", badRequest.Value);
    }

    [Fact]
    public void DevOps_ReturnsOk_WhenRequestIsValid_UsingPrimaryApiKeyHeader()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-API-KEY",
            headerValue: "expected-key",
            jwtHeaderValue: CreateUnsignedJwt("tx-ok-primary"));

        var result = controller.DevOps(new RequestDevOps { To = "Juan" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var message = GetMessageFromAnonymousObject(ok.Value);
        Assert.Equal("Hello Juan your message will be sent", message);
    }

    [Fact]
    public void DevOps_ReturnsOk_WhenRequestIsValid_UsingAlternateApiKeyHeader()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-Parse-REST-API-Key",
            headerValue: "expected-key",
            jwtHeaderValue: CreateUnsignedJwt("tx-ok-alternate"));

        var result = controller.DevOps(new RequestDevOps { To = "Maria" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var message = GetMessageFromAnonymousObject(ok.Value);
        Assert.Equal("Hello Maria your message will be sent", message);
    }

    [Fact]
    public void DevOpsInvalidMethod_ReturnsErrorContent()
    {
        var controller = CreateController(configApiKey: "expected-key");

        var result = controller.DevOpsInvalidMethod();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("ERROR", content.Content);
        Assert.Equal("text/plain", content.ContentType);
    }

    [Fact]
    public void DevOps_ReturnsUnauthorized_WhenJwtHeaderIsMissing()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-Parse-REST-API-Key",
            headerValue: "expected-key");

        var result = controller.DevOps(new RequestDevOps { To = "Juan" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("ERROR", unauthorized.Value);
    }

    [Fact]
    public void DevOps_ReturnsUnauthorized_WhenJwtIsInvalid()
    {
        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-Parse-REST-API-Key",
            headerValue: "expected-key",
            jwtHeaderValue: "invalid.jwt");

        var result = controller.DevOps(new RequestDevOps { To = "Juan" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("ERROR", unauthorized.Value);
    }

    [Fact]
    public void DevOps_ReturnsUnauthorized_WhenJwtJtiIsReused()
    {
        const string reusedToken = "reused-token";
        var jwt = CreateUnsignedJwt(reusedToken);

        var controller = CreateController(
            configApiKey: "expected-key",
            headerName: "X-Parse-REST-API-Key",
            headerValue: "expected-key",
            jwtHeaderValue: jwt);

        var firstCall = controller.DevOps(new RequestDevOps { To = "Juan" });
        Assert.IsType<OkObjectResult>(firstCall);

        var secondCall = controller.DevOps(new RequestDevOps { To = "Juan" });
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(secondCall);
        Assert.Equal("ERROR", unauthorized.Value);
    }

    private static HomeController CreateController(
        string configApiKey,
        string? headerName = null,
        string? headerValue = null,
        string? jwtHeaderValue = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ApiKey"] = configApiKey
            })
            .Build();

        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var controller = new HomeController(configuration, NullLogger<HomeController>.Instance, memoryCache)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (!string.IsNullOrWhiteSpace(headerName) && headerValue is not null)
        {
            controller.Request.Headers[headerName] = headerValue;
        }

        if (!string.IsNullOrWhiteSpace(jwtHeaderValue))
        {
            controller.Request.Headers["X-JWT-KWY"] = jwtHeaderValue;
        }

        return controller;
    }

    private static string CreateUnsignedJwt(string jti)
    {
        var headerJson = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
        var payloadJson = $"{{\"jti\":\"{jti}\",\"exp\":{DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()}}}";

        static string ToBase64Url(string raw)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        return $"{ToBase64Url(headerJson)}.{ToBase64Url(payloadJson)}.";
    }

    private static string? GetMessageFromAnonymousObject(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var messageProperty = value.GetType().GetProperty("message");
        return messageProperty?.GetValue(value)?.ToString();
    }
}