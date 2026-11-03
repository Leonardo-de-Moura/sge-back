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

public class RegistrationsControllerCapacityTests
{
    [Fact]
    public async Task Register_WithAvailableSlot_CreatesRegistrationAttendanceAndUpdatesCounter()
    {
        await using var context = CreateContext();
        await SeedAsync(context);

        var result = await CreateController(context, "student-1")
            .Register(new CreateRegistrationDto { EventId = "event-1" });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        Assert.Equal(1, await context.Registrations.CountAsync(r => r.Status == "confirmado"));
        Assert.Equal(1, await context.Attendances.CountAsync(a => a.UserId == "student-1"));
        Assert.Equal("pendente", (await context.Attendances.SingleAsync()).Status);
        Assert.Equal(1, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
    }

    [Fact]
    public async Task Register_WhenEventIsFull_ReturnsConflictWithoutCreatingRecords()
    {
        await using var context = CreateContext();
        await SeedAsync(context, totalSlots: 1, registrations:
        [
            CreateRegistration("registration-existing", "student-2")
        ]);

        var result = await CreateController(context, "student-1")
            .Register(new CreateRegistrationDto { EventId = "event-1" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(await context.Registrations.ToListAsync());
        Assert.Empty(await context.Attendances.ToListAsync());
        Assert.Equal(1, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
    }

    [Fact]
    public async Task Register_WhenStudentAlreadyHasActiveRegistration_ReturnsConflict()
    {
        await using var context = CreateContext();
        await SeedAsync(context, registrations:
        [
            CreateRegistration("registration-existing", "student-1")
        ]);

        var result = await CreateController(context, "student-1")
            .Register(new CreateRegistrationDto { EventId = "event-1" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(await context.Registrations.ToListAsync());
        Assert.Equal(1, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
    }

    [Fact]
    public async Task Register_AfterCancellation_ReactivatesExistingRegistrationAndPreservesHistory()
    {
        await using var context = CreateContext();
        var registration = CreateRegistration("registration-canceled", "student-1");
        registration.Status = "cancelado";
        registration.CreatedAt = DateTime.UtcNow.AddDays(-4);
        registration.RegistrationDate = DateTime.UtcNow.AddDays(-4);
        registration.TicketCode = "existing-ticket";
        var attendance = CreateAttendance("attendance-1", "student-1");
        attendance.Status = "ausente";
        await SeedAsync(context, registrations: [registration], attendances: [attendance]);

        var result = await CreateController(context, "student-1")
            .Register(new CreateRegistrationDto { EventId = "event-1" });

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(201, response.StatusCode);
        var reactivated = await context.Registrations.SingleAsync();
        Assert.Equal(registration.Id, reactivated.Id);
        Assert.Equal(registration.CreatedAt, reactivated.CreatedAt);
        Assert.Equal(registration.RegistrationDate, reactivated.RegistrationDate);
        Assert.Equal(registration.TicketCode, reactivated.TicketCode);
        Assert.Equal("confirmado", reactivated.Status);
        Assert.NotNull(reactivated.UpdatedAt);
        Assert.Single(await context.Attendances.ToListAsync());
        Assert.Equal("ausente", (await context.Attendances.SingleAsync()).Status);
        Assert.Equal(1, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
    }

    [Fact]
    public async Task Cancel_ReleasesSlotAndRepeatedCancellationDoesNotDecrementAgain()
    {
        await using var context = CreateContext();
        await SeedAsync(context, registrations:
        [
            CreateRegistration("registration-1", "student-1")
        ], attendances:
        [
            CreateAttendance("attendance-1", "student-1")
        ]);
        var controller = CreateController(context, "student-1");

        var cancelResult = await controller.CancelRegistration("registration-1");

        Assert.IsType<OkObjectResult>(cancelResult.Result);
        Assert.Equal("cancelado", (await context.Registrations.SingleAsync()).Status);
        Assert.Equal(0, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
        Assert.Equal("ausente", (await context.Attendances.SingleAsync()).Status);

        var repeatedResult = await controller.CancelRegistration("registration-1");

        Assert.IsType<BadRequestObjectResult>(repeatedResult.Result);
        Assert.Equal(0, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
        Assert.Single(await context.Registrations.ToListAsync());
    }

    [Fact]
    public async Task Register_AfterCancellationCanUseReleasedSlot()
    {
        await using var context = CreateContext();
        var canceled = CreateRegistration("registration-canceled", "student-1");
        canceled.Status = "cancelado";
        await SeedAsync(context, totalSlots: 1, registrations: [canceled]);

        var result = await CreateController(context, "student-2")
            .Register(new CreateRegistrationDto { EventId = "event-1" });

        Assert.Equal(201, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        Assert.Equal(1, await context.Registrations.CountAsync(r => r.Status == "confirmado"));
        Assert.Equal(1, (await context.Events.FindAsync("event-1"))!.EnrolledSlots);
    }

    private static async Task SeedAsync(
        AppDbContext context,
        int totalSlots = 2,
        Registration[]? registrations = null,
        Attendance[]? attendances = null)
    {
        context.Users.AddRange(CreateUser("student-1"), CreateUser("student-2"));
        context.Events.Add(CreateEvent(totalSlots));
        if (registrations != null)
        {
            context.Registrations.AddRange(registrations);
        }

        if (attendances != null)
        {
            context.Attendances.AddRange(attendances);
        }

        await context.SaveChangesAsync();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static RegistrationsController CreateController(AppDbContext context, string userId)
    {
        var controller = new RegistrationsController(context);
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

    private static User CreateUser(string id) => new()
    {
        Id = id,
        Name = id,
        Email = $"{id}@ifce.edu.br",
        PasswordHash = "not-used",
        Role = "Aluno"
    };

    private static Event CreateEvent(int totalSlots) => new()
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
        TotalSlots = totalSlots,
        Status = "Aberto"
    };

    private static Registration CreateRegistration(string id, string userId) => new()
    {
        Id = id,
        UserId = userId,
        EventId = "event-1",
        EventTitle = "Evento",
        RegistrationDate = DateTime.UtcNow,
        TicketCode = $"ticket-{id}",
        Status = "confirmado",
        CreatedAt = DateTime.UtcNow
    };

    private static Attendance CreateAttendance(string id, string userId) => new()
    {
        Id = id,
        EventId = "event-1",
        UserId = userId,
        ParticipantName = userId,
        ParticipantEmail = $"{userId}@ifce.edu.br",
        Matricula = "20260001",
        Status = "pendente"
    };
}
