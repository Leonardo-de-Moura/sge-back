using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var controller = CreateController(context);

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

    private static AuthController CreateController(AppDbContext context)
    {
        var config = new ConfigurationBuilder().Build();
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
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string toEmail, string resetLink) => Task.CompletedTask;
        public Task SendAccountConfirmationAsync(string toEmail, string confirmationLink) => Task.CompletedTask;
    }
}
