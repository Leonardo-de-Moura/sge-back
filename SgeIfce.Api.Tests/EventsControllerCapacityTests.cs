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

public class EventsControllerCapacityTests
{
    [Fact]
    public async Task UpdateEvent_CannotReduceCapacityBelowConfirmedRegistrations()
    {
        await using var context = CreateContext(confirmedRegistrations: 2);
        var result = await CreateController(context).UpdateEvent(
            "event-1",
            new UpdateEventDto { TotalSlots = 1 });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(3, (await context.Events.FindAsync("event-1"))!.TotalSlots);
    }

    [Fact]
    public async Task UpdateEvent_AllowsCapacityReductionWhenEnoughSlotsRemain()
    {
        await using var context = CreateContext(confirmedRegistrations: 1);
        var result = await CreateController(context).UpdateEvent(
            "event-1",
            new UpdateEventDto { TotalSlots = 2 });

        Assert.IsType<OkObjectResult>(result.Result);
        var updated = await context.Events.FindAsync("event-1");
        Assert.Equal(2, updated!.TotalSlots);
        Assert.Equal(1, updated.EnrolledSlots);
    }

    private static AppDbContext CreateContext(int confirmedRegistrations)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Events.Add(new Event
        {
            Id = "event-1",
            Title = "Evento",
            Description = "Descrição",
            Category = "Palestra",
            Modality = "Presencial",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            Workload = "2 horas",
            Location = "Auditório",
            TotalSlots = 3,
            EnrolledSlots = 0,
            Status = "Aberto",
            DayMonth = "04 NOV",
            OrganizerId = "teacher-1"
        });

        for (var index = 0; index < confirmedRegistrations; index++)
        {
            context.Registrations.Add(new Registration
            {
                Id = $"registration-{index}",
                UserId = $"student-{index}",
                EventId = "event-1",
                EventTitle = "Evento",
                RegistrationDate = DateTime.UtcNow,
                TicketCode = $"ticket-{index}",
                Status = "confirmado"
            });
        }

        context.SaveChanges();
        return context;
    }

    private static EventsController CreateController(AppDbContext context)
    {
        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "teacher-1"),
                    new Claim(ClaimTypes.Role, "Professor")
                ], "Test"))
            }
        };
        return controller;
    }
}
