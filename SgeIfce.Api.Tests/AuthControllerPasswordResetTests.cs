using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;

namespace SgeIfce.Api.Tests;

public class AuthControllerPasswordResetTests
{
    [Fact]
    public async Task ForgotPassword_WhenUserExists_CreatesResetToken()
    {
        await using var context = CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Usuário Teste",
            Email = "teste@ifce.edu.br",
            PasswordHash = new BcryptPasswordHasher().Hash("Senha@123"),
            Role = "Aluno",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.ForgotPassword(new ForgotPasswordDto { Email = "TESTE@ifce.edu.br" });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotEmpty(context.PasswordResetTokens.Where(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPasswordAndInvalidatesToken()
    {
        await using var context = CreateContext();
        var hasher = new BcryptPasswordHasher();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Usuário Teste",
            Email = "reset@ifce.edu.br",
            PasswordHash = hasher.Hash("SenhaAntiga@123"),
            Role = "Aluno",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var tokenValue = "token-valido-123";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenValue))),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            UsedAt = null
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.ResetPassword(new ResetPasswordDto
        {
            Token = tokenValue,
            NewPassword = "NovaSenha@456",
            ConfirmPassword = "NovaSenha@456"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.True(hasher.Verify("NovaSenha@456", updatedUser!.PasswordHash));
        var token = await context.PasswordResetTokens.SingleAsync(x => x.UserId == user.Id);
        Assert.NotNull(token.UsedAt);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthController CreateController(AppDbContext context)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontendUrl"] = "http://localhost:3000",
                ["PasswordReset:TokenExpirationMinutes"] = "30",
                ["Email:Host"] = "",
                ["Email:Port"] = "587",
                ["Email:Username"] = "",
                ["Email:Password"] = "",
                ["Email:From"] = "noreply@sge-ifce.edu.br",
                ["Email:FromName"] = "SGE-IFCE"
            })
            .Build();

        return new AuthController(
            context,
            new TokenService(config),
            new BcryptPasswordHasher(),
            new NoOpEmailService(),
            new PasswordResetService(context, config),
            config);
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
            => Task.CompletedTask;

        public Task SendPasswordResetAsync(string toEmail, string resetLink)
            => Task.CompletedTask;

        public Task SendAccountConfirmationAsync(string toEmail, string confirmationLink)
            => Task.CompletedTask;
    }
}
