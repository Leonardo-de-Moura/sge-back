using Microsoft.AspNetCore.Http;
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

public class AuthControllerSecurityTests
{
    [Fact]
    public async Task RegisterStudent_RemainsAvailableForPublicRegistration()
    {
        await using var context = CreateContext();
        var emailService = new CapturingEmailService();
        var controller = CreateController(context, emailService);

        var result = await controller.RegisterStudent(new RegisterStudentDto
        {
            Name = "Aluno novo",
            Email = "aluno@ifce.edu.br",
            Matricula = "20260001",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        var createdUser = Assert.Single(await context.Users.ToListAsync());
        Assert.Equal("Aluno", createdUser.Role);
        Assert.False(createdUser.EmailConfirmed);
        var token = Assert.Single(await context.EmailConfirmationTokens.ToListAsync());
        Assert.Equal(64, token.TokenHash.Length);
        Assert.Equal("aluno@ifce.edu.br", emailService.To);
        Assert.Contains("http://localhost:3000/confirmar-email?token=", emailService.Html);
        Assert.Contains("SGE.IFCE", emailService.Html);
        Assert.DoesNotContain(token.TokenHash, emailService.Html);
    }

    [Fact]
    public async Task RegisterStudent_WhenResendFails_ReturnsCreatedAndKeepsUser()
    {
        await using var context = CreateContext();
        var emailService = new CapturingEmailService
        {
            SendException = new HttpRequestException("sensitive Resend response")
        };
        var controller = CreateController(context, emailService);

        var result = await controller.RegisterStudent(new RegisterStudentDto
        {
            Name = "Aluno novo",
            Email = "falha@ifce.edu.br",
            Matricula = "20260002",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        });

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        var response = Assert.IsType<ApiResponse<UserProfileDto>>(created.Value);
        Assert.Contains("não pôde ser enviado", response.Message);
        Assert.Single(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task RegisterTeacher_WithoutAdministrativeApproval_DoesNotCreateAccount()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = controller.RegisterTeacher(new RegisterTeacherDto
        {
            Name = "Docente novo",
            Email = "novo@ifce.edu.br",
            Siape = "123456",
            Password = "Senha123",
            ConfirmPassword = "Senha123"
        });

        var forbidden = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(await context.Users.ToListAsync());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateController(
        AppDbContext context,
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
            new BcryptPasswordHasher(),
            emailService ?? new CapturingEmailService(),
            new EmailConfirmationService(context),
            new PasswordResetService(context, config),
            config,
            NullLogger<AuthController>.Instance);
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public string To { get; private set; } = string.Empty;
        public string Html { get; private set; } = string.Empty;
        public Exception? SendException { get; init; }

        public Task SendAsync(string to, string subject, string html)
        {
            To = to;
            Html = html;
            return SendException == null
                ? Task.CompletedTask
                : Task.FromException(SendException);
        }
    }
}
