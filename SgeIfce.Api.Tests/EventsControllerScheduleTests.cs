using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Tests;

public class EventsControllerScheduleTests
{
    private static readonly DateTime EventDate = new(2026, 9, 27, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateEvent_AllowsNonOverlappingInterval()
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(12, 14));

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Theory]
    [InlineData(8, 12)]
    [InlineData(10, 13)]
    [InlineData(7, 9)]
    [InlineData(7, 13)]
    [InlineData(9, 11)]
    public async Task CreateEvent_RejectsOverlappingInterval(int startHour, int endHour)
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(startHour, endHour));

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<EventResponseDto>>(conflict.Value);
        Assert.Contains("Auditório", Assert.Single(response.Errors!));
    }

    [Theory]
    [InlineData(12, 14)]
    [InlineData(6, 8)]
    public async Task CreateEvent_AllowsTouchingIntervals(int startHour, int endHour)
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(startHour, endHour));

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AllowsDifferentLocation()
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(9, 11, location: "Laboratório"));

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AllowsDifferentDate()
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(9, 11, date: EventDate.AddDays(1)));

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AllowsUnchangedOwnInterval()
    {
        await using var context = CreateContext(ExistingEvent());
        var controller = CreateController(context);

        var result = await controller.UpdateEvent("existing", new UpdateEventDto
        {
            Title = "Título atualizado"
        });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateEvent_RejectsConflictWithAnotherEvent()
    {
        var otherEvent = ExistingEvent("other", startHour: 11, endHour: 15);
        await using var context = CreateContext(ExistingEvent(), otherEvent);
        var controller = CreateController(context);

        var result = await controller.UpdateEvent("existing", new UpdateEventDto
        {
            StartDate = At(10),
            EndDate = At(14)
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(12, 8)]
    public async Task CreateEvent_RejectsInvalidInterval(int startHour, int endHour)
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(startHour, endHour));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateEvent_RejectsLocationDifferencesOnlyByCaseAndWhitespace()
    {
        await using var context = CreateContext(ExistingEvent(location: " Auditório "));
        var controller = CreateController(context);

        var result = await controller.CreateEvent(CreateRequest(9, 11, location: "AUDITÓRIO"));

        Assert.IsType<ConflictObjectResult>(result.Result);
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

    private static EventsController CreateController(AppDbContext context) => new(context)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        }
    };

    private static Event ExistingEvent(
        string id = "existing",
        int startHour = 8,
        int endHour = 12,
        string location = "Auditório") => new()
    {
        Id = id,
        Title = id,
        Description = "Descrição",
        Category = "Palestra",
        Modality = "Presencial",
        StartDate = At(startHour),
        EndDate = At(endHour),
        Workload = "4 horas",
        Location = location,
        TotalSlots = 10,
        Status = "Aberto",
        DayMonth = "27 SET",
        CreatedAt = EventDate
    };

    private static CreateEventDto CreateRequest(
        int startHour,
        int endHour,
        DateTime? date = null,
        string location = "Auditório") => new()
    {
        Title = "Novo evento",
        Description = "Descrição",
        Category = "Palestra",
        Modality = "Presencial",
        StartDate = At(startHour, date),
        EndDate = At(endHour, date),
        Workload = "2 horas",
        Location = location,
        TotalSlots = 10
    };

    private static DateTime At(int hour, DateTime? date = null) =>
        (date ?? EventDate).AddHours(hour);
}