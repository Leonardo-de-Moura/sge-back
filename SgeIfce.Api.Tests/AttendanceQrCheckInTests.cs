using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using Xunit;

namespace SgeIfce.Api.Tests;

public class AttendanceQrCheckInTests
{
    [Fact]
    public async Task CheckIn_WithMatchingAccountAndConfirmedRegistration_MarksParticipantPresent()
    {
        await using var context = CreateContext();
        var (user, eventEntity) = await SeedParticipantAsync(context);
        var controller = CreateController(context, user.Id);

        var result = await controller.CheckIn(new CheckInRequestDto
        {
            Token = eventEntity.QrCodeToken!,
            Name = "  " + user.Name + " ",
            Email = user.Email.ToUpperInvariant()
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<CheckInResponseDto>>(ok.Value);
        Assert.False(response.Data!.AlreadyRegistered);
        Assert.Equal("presente", response.Data.Status);

        var attendance = await context.Attendances.SingleAsync();
        Assert.Equal(user.Id, attendance.UserId);
        Assert.Equal("presente", attendance.Status);
        Assert.NotNull(attendance.CheckedInAt);
    }

    [Theory]
    [InlineData("Pessoa diferente", "aluno@ifce.edu.br")]
    [InlineData("Aluno de teste", "outra-conta@ifce.edu.br")]
    public async Task CheckIn_WithIdentityDifferentFromAuthenticatedAccount_IsRejected(
        string name,
        string email)
    {
        await using var context = CreateContext();
        var (user, eventEntity) = await SeedParticipantAsync(context);
        var controller = CreateController(context, user.Id);

        var result = await controller.CheckIn(new CheckInRequestDto
        {
            Token = eventEntity.QrCodeToken!,
            Name = name,
            Email = email
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await context.Attendances.ToListAsync());
    }

    [Fact]
    public async Task CheckIn_WithoutConfirmedRegistration_IsRejected()
    {
        await using var context = CreateContext();
        var (user, eventEntity) = await SeedParticipantAsync(context, registrationStatus: "cancelado");
        var controller = CreateController(context, user.Id);

        var result = await controller.CheckIn(new CheckInRequestDto
        {
            Token = eventEntity.QrCodeToken!,
            Name = user.Name,
            Email = user.Email
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await context.Attendances.ToListAsync());
    }

    [Fact]
    public async Task CheckIn_WhenAuthenticatedUserIsNotAStudent_IsForbidden()
    {
        await using var context = CreateContext();
        var (user, eventEntity) = await SeedParticipantAsync(context);
        user.Role = "Professor";
        await context.SaveChangesAsync();

        var controller = CreateController(context, user.Id);
        var result = await controller.CheckIn(new CheckInRequestDto
        {
            Token = eventEntity.QrCodeToken!,
            Name = user.Name,
            Email = user.Email
        });

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Empty(await context.Attendances.ToListAsync());
    }

    [Fact]
    public async Task CheckIn_WhenAlreadyPresent_IsIdempotent()
    {
        await using var context = CreateContext();
        var (user, eventEntity) = await SeedParticipantAsync(context);
        var checkedInAt = DateTime.UtcNow.AddMinutes(-2);
        context.Attendances.Add(new Attendance
        {
            Id = "attendance-1",
            EventId = eventEntity.Id,
            UserId = user.Id,
            ParticipantName = user.Name,
            ParticipantEmail = user.Email,
            Matricula = "2026000001",
            Status = "presente",
            CheckedInAt = checkedInAt
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, user.Id);
        var result = await controller.CheckIn(new CheckInRequestDto
        {
            Token = eventEntity.QrCodeToken!,
            Name = user.Name,
            Email = user.Email
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<CheckInResponseDto>>(ok.Value);
        Assert.True(response.Data!.AlreadyRegistered);
        Assert.Equal(checkedInAt, response.Data.CheckedInAt);
        Assert.Single(await context.Attendances.ToListAsync());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(User User, Event Event)> SeedParticipantAsync(
        AppDbContext context,
        string registrationStatus = "confirmado")
    {
        var user = new User
        {
            Id = "user-1",
            Name = "Aluno de teste",
            Email = "aluno@ifce.edu.br",
            PasswordHash = "not-used",
            Role = "Aluno",
            Matricula = "2026000001"
        };
        var eventEntity = new Event
        {
            Id = "event-1",
            Title = "Evento de teste",
            Description = "Evento para teste do check-in",
            Category = "Palestra",
            Modality = "Presencial",
            Workload = "4 horas",
            Location = "Campus",
            DayMonth = "29/10",
            Status = "Aberto",
            QrCodeToken = "secure-event-token",
            QrCodeExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        context.Users.Add(user);
        context.Events.Add(eventEntity);
        context.Registrations.Add(new Registration
        {
            Id = "registration-1",
            UserId = user.Id,
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            RegistrationDate = DateTime.UtcNow,
            TicketCode = "ticket-1",
            Status = registrationStatus
        });
        await context.SaveChangesAsync();

        return (user, eventEntity);
    }

    private static AttendanceController CreateController(AppDbContext context, string userId)
    {
        var controller = new AttendanceController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId)
                ], "Test"))
            }
        };
        return controller;
    }
}
