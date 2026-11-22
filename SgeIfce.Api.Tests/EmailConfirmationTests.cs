using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;
using Xunit;

namespace SgeIfce.Api.Tests;

public class EmailConfirmationTests
{
    [Fact]
    public async Task CreateToken_StoresOnlyHashAndExpiresAfter24Hours()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);

        var rawToken = await new EmailConfirmationService(context).CreateTokenAsync(user);

        var token = await context.EmailConfirmationTokens.SingleAsync();
        Assert.Equal(64, rawToken.Length);
        Assert.All(rawToken, character => Assert.True(Uri.IsHexDigit(character)));
        Assert.Equal(HashToken(rawToken), token.TokenHash);
        Assert.NotEqual(rawToken, token.TokenHash);
        Assert.Equal(TimeSpan.FromHours(24), token.ExpiresAt - token.CreatedAt);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_ConfirmsUserAndInvalidatesToken()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        const string rawToken = "A8" + "B1" + "C2" + "D3" + "E4" + "F5" + "A6" + "B7" +
                                "C8" + "D9" + "E0" + "F1" + "A2" + "B3" + "C4" + "D5" +
                                "E6" + "F7" + "A8" + "B9" + "C0" + "D1" + "E2" + "F3" +
                                "A4" + "B5" + "C6" + "D7" + "E8" + "F9" + "A0" + "B1";
        var confirmationToken = new EmailConfirmationToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        context.EmailConfirmationTokens.Add(confirmationToken);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.ConfirmEmail(
            new ConfirmEmailDto { Token = rawToken.ToLowerInvariant() });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(user.EmailConfirmedAt);
        Assert.NotNull(confirmationToken.UsedAt);

        var reusedResult = await controller.ConfirmEmail(new ConfirmEmailDto { Token = rawToken });
        var repeatedConfirmation = Assert.IsType<OkObjectResult>(reusedResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(repeatedConfirmation.Value);
        Assert.Equal("E-mail já confirmado.", response.Message);
    }

    [Fact]
    public async Task ConfirmEmail_WithExpiredToken_DoesNotConfirmUser()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        var rawToken = new string('A', 64);
        context.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).ConfirmEmail(
            new ConfirmEmailDto { Token = rawToken });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_WithTokenInvalidatedBeforeConfirmation_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        var invalidatedAt = DateTime.UtcNow.AddMinutes(-2);
        user.EmailConfirmed = true;
        user.EmailConfirmedAt = DateTime.UtcNow;
        context.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = HashToken(new string('B', 64)),
            CreatedAt = invalidatedAt.AddMinutes(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UsedAt = invalidatedAt
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).ConfirmEmail(
            new ConfirmEmailDto { Token = new string('B', 64) });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ResendConfirmation_ReplacesPreviousTokenForUnconfirmedUser()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        var confirmationService = new EmailConfirmationService(context);
        await confirmationService.CreateTokenAsync(user);
        var previousToken = await context.EmailConfirmationTokens.SingleAsync();
        previousToken.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
        await context.SaveChangesAsync();

        var emailService = new CapturingEmailService();
        var result = await CreateController(context, emailService: emailService).ResendConfirmation(
            new ResendConfirmationDto { Email = user.Email });

        Assert.IsType<OkObjectResult>(result.Result);
        var tokens = await context.EmailConfirmationTokens
            .Where(token => token.UserId == user.Id)
            .ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Single(tokens.Where(token => token.UsedAt == null));
        Assert.Single(tokens.Where(token => token.UsedAt != null));
        Assert.Equal(1, emailService.SendCount);
    }

    [Fact]
    public async Task ResendConfirmation_ForUnknownUserAndSendFailure_ReturnsSameGenericResponse()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        var failingEmailService = new CapturingEmailService
        {
            SendException = new HttpRequestException("sensitive Resend response")
        };
        var controller = CreateController(context, emailService: failingEmailService);

        var unknownResult = await controller.ResendConfirmation(
            new ResendConfirmationDto { Email = "missing@ifce.edu.br" });
        var failureResult = await controller.ResendConfirmation(
            new ResendConfirmationDto { Email = user.Email });

        var unknownResponse = Assert.IsType<ApiResponse<object>>(
            Assert.IsType<OkObjectResult>(unknownResult.Result).Value);
        var failureResponse = Assert.IsType<ApiResponse<object>>(
            Assert.IsType<OkObjectResult>(failureResult.Result).Value);
        Assert.Equal(200, Assert.IsType<OkObjectResult>(unknownResult.Result).StatusCode);
        Assert.Equal(200, Assert.IsType<OkObjectResult>(failureResult.Result).StatusCode);
        Assert.Equal(
            JsonSerializer.Serialize(unknownResponse),
            JsonSerializer.Serialize(failureResponse));
        Assert.Equal(1, failingEmailService.SendCount);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailSendFails_ReturnsGenericSuccess()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        var emailService = new CapturingEmailService
        {
            SendException = new HttpRequestException("sensitive Resend response")
        };

        var result = await CreateController(context, emailService: emailService)
            .ForgotPassword(new ForgotPasswordDto { Email = user.Email });

        var response = Assert.IsType<ApiResponse<object>>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.True(response.Success);
        Assert.Equal(1, emailService.SendCount);
    }

    [Fact]
    public async Task ResendConfirmation_WhenTokenIsLessThanOneMinuteOld_DoesNotSendAnotherEmail()
    {
        await using var context = CreateContext();
        var user = await CreateUserAsync(context);
        await new EmailConfirmationService(context).CreateTokenAsync(user);
        var emailService = new CapturingEmailService();

        var result = await CreateController(context, emailService: emailService).ResendConfirmation(
            new ResendConfirmationDto { Email = user.Email });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(0, emailService.SendCount);
        Assert.Single(await context.EmailConfirmationTokens.ToListAsync());
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_IsRejected()
    {
        await using var context = CreateContext();
        var passwordHasher = new BcryptPasswordHasher();
        var user = await CreateUserAsync(context, passwordHasher);

        var result = await CreateController(context, passwordHasher).Login(
            new LoginRequestDto { Email = user.Email, Password = "Senha123" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<LoginResponseDto>>(unauthorized.Value);
        Assert.Equal("E-mail não confirmado.", response.Message);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<User> CreateUserAsync(
        AppDbContext context,
        IPasswordHasher? passwordHasher = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Usuário de teste",
            Email = $"teste-{Guid.NewGuid():N}@ifce.edu.br",
            PasswordHash = (passwordHasher ?? new BcryptPasswordHasher()).Hash("Senha123"),
            Role = "Aluno"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static AuthController CreateController(
        AppDbContext context,
        IPasswordHasher? passwordHasher = null,
        IEmailService? emailService = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrl"] = "http://localhost:3000"
            })
            .Build();
        return new AuthController(
            context,
            new TokenService(config),
            passwordHasher ?? new BcryptPasswordHasher(),
            emailService ?? new CapturingEmailService(),
            new EmailConfirmationService(context),
            new PasswordResetService(context, config),
            config,
            NullLogger<AuthController>.Instance);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public int SendCount { get; private set; }
        public Exception? SendException { get; init; }

        public Task SendAsync(string to, string subject, string html)
        {
            SendCount++;
            return SendException == null
                ? Task.CompletedTask
                : Task.FromException(SendException);
        }
    }
}
