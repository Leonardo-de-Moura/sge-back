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

public class EventsControllerOwnershipTests
{
    [Fact]
    public async Task UpdateEvent_NonOwnerCannotModifyEvent()
    {
        await using var context = CreateContext(CreateEvent());
        var controller = CreateController(context, "teacher-2");

        var result = await controller.UpdateEvent("event-1", new UpdateEventDto { Title = "Alterado" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Evento original", (await context.Events.FindAsync("event-1"))!.Title);
    }

    [Fact]
    public async Task DeleteEvent_NonOwnerCannotCloseEvent()
    {
        await using var context = CreateContext(CreateEvent());
        var controller = CreateController(context, "teacher-2");

        var result = await controller.DeleteEvent("event-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Aberto", (await context.Events.FindAsync("event-1"))!.Status);
    }

    [Fact]
    public async Task UpdateEvent_EventWithoutOrganizerCannotBeModified()
    {
        var eventEntity = CreateEvent();
        eventEntity.OrganizerId = null;
        await using var context = CreateContext(eventEntity);
        var controller = CreateController(context, "teacher-1");

        var result = await controller.UpdateEvent("event-1", new UpdateEventDto { Title = "Alterado" });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Evento original", (await context.Events.FindAsync("event-1"))!.Title);
    }

    [Fact]
    public async Task GenerateEventQrCode_NonOwnerCannotChangeQrCode()
    {
        await using var context = CreateContext(CreateEvent());
        var controller = CreateController(context, "teacher-2");

        var result = await controller.GenerateEventQrCode("event-1", new GenerateQrCodeRequestDto
        {
            ForceGenerate = true
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Null((await context.Events.FindAsync("event-1"))!.QrCodeToken);
    }

    [Fact]
    public async Task GetEventQrCode_NonOwnerCannotReadQrCode()
    {
        var eventEntity = CreateEvent();
        eventEntity.QrCodeToken = "private-token";
        eventEntity.QrCodeExpiresAt = DateTime.UtcNow.AddHours(1);
        await using var context = CreateContext(eventEntity);
        var controller = CreateController(context, "teacher-2");

        var result = await controller.GetEventQrCode("event-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEventAttendance_NonOwnerCannotReadParticipants()
    {
        await using var context = CreateContext(CreateEvent());
        var controller = CreateController(context, "teacher-2");

        var result = await controller.GetEventAttendance("event-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDashboardStats_OnlyCountsOwnedEvents()
    {
        await using var context = CreateContext(CreateEvent());
        var controller = CreateController(context, "teacher-2");

        var result = await controller.GetDashboardStats();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DashboardStatsDto>>(ok.Value);
        Assert.Equal(0, response.Data!.EventsUnderManagement);
    }

    private static AppDbContext CreateContext(params Event[] events)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Events.AddRange(events);
        context.SaveChanges();
        return context;
    }

    private static EventsController CreateController(AppDbContext context, string userId)
    {
        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, "Professor")
                ], "Test"))
            }
        };
        return controller;
    }

    private static Event CreateEvent() => new()
    {
        Id = "event-1",
        Title = "Evento original",
        Description = "Descrição",
        Category = "Palestra",
        Modality = "Presencial",
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
        Workload = "2 horas",
        Location = "Auditório",
        TotalSlots = 20,
        Status = "Aberto",
        DayMonth = "04 NOV",
        OrganizerId = "teacher-1"
    };
}
