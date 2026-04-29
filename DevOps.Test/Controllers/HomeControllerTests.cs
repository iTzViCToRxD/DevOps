using DevOps.Controllers;
using DevOps.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
            headerValue: "invalid-key");

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
            headerValue: "expected-key");

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
            headerValue: "expected-key");

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
            headerValue: "expected-key");

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
            headerValue: "expected-key");

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

    private static HomeController CreateController(string configApiKey, string? headerName = null, string? headerValue = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:ApiKey"] = configApiKey
            })
            .Build();

        var controller = new HomeController(configuration, NullLogger<HomeController>.Instance)
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

        return controller;
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