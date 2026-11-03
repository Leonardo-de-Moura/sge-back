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

public class AttendanceControllerOwnershipTests
{
    [Fact]
    public async Task GetAttendancesByEvent_NonOwnerCannotReadParticipants()
    {
        await using var context = CreateContext();
        await SeedAttendanceAsync(context);
        var controller = CreateController(context);

        var result = await controller.GetAttendancesByEvent("event-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_NonOwnerCannotChangeAttendance()
    {
        await using var context = CreateContext();
        await SeedAttendanceAsync(context);
        var controller = CreateController(context);

        var result = await controller.UpdateStatus("attendance-1", new UpdateAttendanceDto
        {
            Status = "presente"
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("pendente", (await context.Attendances.FindAsync("attendance-1"))!.Status);
    }

    [Fact]
    public async Task BulkUpdate_NonOwnerCannotChangeAttendance()
    {
        await using var context = CreateContext();
        await SeedAttendanceAsync(context);
        var controller = CreateController(context);

        var result = await controller.BulkUpdate(new BulkAttendanceDto
        {
            EventId = "event-1",
            Attendances = [new BulkAttendanceItemDto { Id = "attendance-1", Status = "presente" }]
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("pendente", (await context.Attendances.FindAsync("attendance-1"))!.Status);
    }

    private static async Task SeedAttendanceAsync(AppDbContext context)
    {
        context.Events.Add(new Event
        {
            Id = "event-1",
            Title = "Evento",
            Description = "Descrição",
            Category = "Palestra",
            Modality = "Presencial",
            Workload = "2 horas",
            Location = "Campus",
            TotalSlots = 10,
            Status = "Aberto",
            DayMonth = "04 NOV",
            OrganizerId = "teacher-1"
        });
        context.Attendances.Add(new Attendance
        {
            Id = "attendance-1",
            EventId = "event-1",
            ParticipantName = "Aluno",
            ParticipantEmail = "aluno@ifce.edu.br",
            Matricula = "123",
            Status = "pendente"
        });
        await context.SaveChangesAsync();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AttendanceController CreateController(AppDbContext context)
    {
        var controller = new AttendanceController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "teacher-2"),
                    new Claim(ClaimTypes.Role, "Professor")
                ], "Test"))
            }
        };
        return controller;
    }
}
