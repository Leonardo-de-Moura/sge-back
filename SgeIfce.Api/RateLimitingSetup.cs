using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace SgeIfce.Api;

public static class RateLimitingSetup
{
    public const string RegisterPolicy = "register";
    public const string ResendConfirmationPolicy = "resend-confirmation";
    public const string ForgotPasswordPolicy = "forgot-password";

    public static void AddConfiguredRateLimiting(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                RegisterPolicy,
                context => GetPartition(
                    context.Connection.RemoteIpAddress,
                    configuration,
                    "Register",
                    defaultPermitLimit: 30));
            options.AddPolicy(
                ResendConfirmationPolicy,
                context => GetPartition(
                    context.Connection.RemoteIpAddress,
                    configuration,
                    "ResendConfirmation",
                    defaultPermitLimit: 10));
            options.AddPolicy(
                ForgotPasswordPolicy,
                context => GetPartition(
                    context.Connection.RemoteIpAddress,
                    configuration,
                    "ForgotPassword",
                    defaultPermitLimit: 10));
        });
    }

    private static RateLimitPartition<string> GetPartition(
        IPAddress? remoteIpAddress,
        IConfiguration configuration,
        string sectionName,
        int defaultPermitLimit)
    {
        var section = configuration.GetSection($"RateLimiting:{sectionName}");
        var permitLimit = section.GetValue<int?>("PermitLimit") ?? defaultPermitLimit;
        var windowMinutes = section.GetValue<int?>("WindowMinutes") ?? 15;

        if (permitLimit < 1 || windowMinutes < 1)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{sectionName} deve definir limites positivos.");
        }

        var partitionKey = remoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
