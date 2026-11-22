using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SgeIfce.Api;
using SgeIfce.Api.Controllers;
using Xunit;

namespace SgeIfce.Api.Tests;

public class RateLimitingTests
{
    [Theory]
    [InlineData("/register", 30)]
    [InlineData("/resend-confirmation", 10)]
    [InlineData("/forgot-password", 10)]
    public async Task Policy_Returns429AfterConfiguredPermitLimit(string path, int permitLimit)
    {
        await using var server = await RateLimitedTestServer.StartAsync();

        for (var request = 0; request < permitLimit; request++)
        {
            using var allowed = await server.Client.PostAsync(path, content: null);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        using var rejected = await server.Client.PostAsync(path, content: null);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task ExhaustingOnePolicy_DoesNotConsumeOtherPoliciesQuota()
    {
        await using var server = await RateLimitedTestServer.StartAsync();

        for (var request = 0; request < 30; request++)
        {
            using var response = await server.Client.PostAsync("/register", content: null);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        using var registerRejected =
            await server.Client.PostAsync("/register", content: null);
        using var resendAllowed =
            await server.Client.PostAsync("/resend-confirmation", content: null);
        using var forgotPasswordAllowed =
            await server.Client.PostAsync("/forgot-password", content: null);

        Assert.Equal(HttpStatusCode.TooManyRequests, registerRejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resendAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, forgotPasswordAllowed.StatusCode);
    }

    [Fact]
    public async Task Policy_UsesPermitLimitFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Register:PermitLimit"] = "2"
            })
            .Build();
        await using var server = await RateLimitedTestServer.StartAsync(configuration);

        using var first = await server.Client.PostAsync("/register", content: null);
        using var second = await server.Client.PostAsync("/register", content: null);
        using var rejected = await server.Client.PostAsync("/register", content: null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public void AuthEndpoints_UseTheirDedicatedRateLimitPolicies()
    {
        AssertPolicy(nameof(AuthController.RegisterStudent), RateLimitingSetup.RegisterPolicy);
        AssertPolicy(
            nameof(AuthController.ResendConfirmation),
            RateLimitingSetup.ResendConfirmationPolicy);
        AssertPolicy(
            nameof(AuthController.ForgotPassword),
            RateLimitingSetup.ForgotPasswordPolicy);
    }

    private static void AssertPolicy(string methodName, string policyName)
    {
        var method = typeof(AuthController).GetMethod(methodName);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(policyName, attribute!.PolicyName);
    }

    private sealed class RateLimitedTestServer : IAsyncDisposable
    {
        private readonly WebApplication _application;

        private RateLimitedTestServer(WebApplication application, HttpClient client)
        {
            _application = application;
            Client = client;
        }

        public HttpClient Client { get; }

        public static async Task<RateLimitedTestServer> StartAsync(
            IConfiguration? configuration = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            RateLimitingSetup.AddConfiguredRateLimiting(
                builder.Services,
                configuration ?? new ConfigurationBuilder().Build());

            var application = builder.Build();
            application.UseRouting();
            application.UseRateLimiter();
            application.MapPost("/register", () => Results.StatusCode(201))
                .RequireRateLimiting(RateLimitingSetup.RegisterPolicy);
            application.MapPost("/resend-confirmation", () => Results.Ok())
                .RequireRateLimiting(RateLimitingSetup.ResendConfirmationPolicy);
            application.MapPost("/forgot-password", () => Results.Ok())
                .RequireRateLimiting(RateLimitingSetup.ForgotPasswordPolicy);

            await application.StartAsync();
            var server = application.Services.GetRequiredService<IServer>();
            var address = server.Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();

            return new RateLimitedTestServer(
                application,
                new HttpClient { BaseAddress = new Uri(address) });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _application.DisposeAsync();
        }
    }
}
