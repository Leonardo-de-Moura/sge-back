using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.Models;
using Xunit;

namespace SgeIfce.Api.Tests;

public class RegistrationsControllerSecurityTests
{
    [Fact]
    public async Task GetTicket_OtherUsersRegistration_IsNotFound()
    {
        await using var context = CreateContext();
        await SeedRegistrationAsync(context);
        var controller = CreateController(context, "user-2");

        var result = await controller.GetTicket("registration-1");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTicket_OwnRegistration_IsReturned()
    {
        await using var context = CreateContext();
        await SeedRegistrationAsync(context);
        var controller = CreateController(context, "user-1");

        var result = await controller.GetTicket("registration-1");

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static async Task SeedRegistrationAsync(AppDbContext context)
    {
        context.Users.AddRange(
            CreateUser("user-1", "one@ifce.edu.br"),
            CreateUser("user-2", "two@ifce.edu.br"));
        context.Events.Add(CreateEvent());
        context.Registrations.Add(new Registration
        {
            Id = "registration-1",
            UserId = "user-1",
            EventId = "event-1",
            EventTitle = "Evento",
            TicketCode = "ticket-1",
            Status = "confirmado",
            RegistrationDate = DateTime.UtcNow
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

    private static User CreateUser(string id, string email) => new()
    {
        Id = id,
        Name = id,
        Email = email,
        PasswordHash = "not-used",
        Role = "Aluno"
    };

    private static Event CreateEvent() => new()
    {
        Id = "event-1",
        Title = "Evento",
        Description = "Descrição",
        Category = "Palestra",
        Modality = "Presencial",
        Workload = "4 horas",
        Location = "Campus",
        DayMonth = "03 NOV",
        Status = "Aberto"
    };
}
