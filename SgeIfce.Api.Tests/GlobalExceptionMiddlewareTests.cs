using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SgeIfce.Api.Middleware;
using Xunit;

namespace SgeIfce.Api.Tests;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task UnauthorizedResponse_UsesCamelCaseApiEnvelope()
    {
        var context = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var middleware = new GlobalExceptionMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            NullLogger<GlobalExceptionMiddleware>.Instance
        );

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var response = await JsonDocument.ParseAsync(responseBody);

        Assert.False(response.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Acesso não autorizado.", response.RootElement.GetProperty("message").GetString());
        Assert.True(response.RootElement.GetProperty("errors").GetArrayLength() > 0);
    }
}
