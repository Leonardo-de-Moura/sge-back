using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SgeIfce.Api.Controllers;
using SgeIfce.Api.Data;
using SgeIfce.Api.DTOs;
using SgeIfce.Api.Models;
using Xunit;

namespace SgeIfce.Api.Tests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("SGE_TEST_POSTGRES_CONNECTION_STRING")))
        {
            Skip = "Set SGE_TEST_POSTGRES_CONNECTION_STRING to the isolated sge_phase2_test database.";
        }
    }
}

public class PostgresRegistrationConcurrencyTests
{
    [PostgreSqlFact]
    public async Task MigrationsAndConcurrentRequests_EnforceCapacityAndActiveUniqueness()
    {
        var connectionString = Environment.GetEnvironmentVariable("SGE_TEST_POSTGRES_CONNECTION_STRING")!;
        var connectionDetails = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("sge_phase2_test", connectionDetails.Database);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var suffix = Guid.NewGuid().ToString("N");
        var firstEventId = $"capacity-{suffix}";
        var secondEventId = $"duplicate-{suffix}";
        var firstStudentId = $"student-a-{suffix}";
        var secondStudentId = $"student-b-{suffix}";
        var eventStart = DateTime.UtcNow.AddDays(1);

        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Users.AddRange(
                CreateUser(firstStudentId, $"student-a-{suffix}@ifce.edu.br"),
                CreateUser(secondStudentId, $"student-b-{suffix}@ifce.edu.br"));
            seedContext.Events.AddRange(
                CreateEvent(firstEventId, "Evento capacidade", 1, eventStart),
                CreateEvent(secondEventId, "Evento duplicidade", 2, eventStart.AddDays(1)));
            await seedContext.SaveChangesAsync();
        }

        var lastSlotResults = await Task.WhenAll(
            RegisterAsync(options, firstEventId, firstStudentId),
            RegisterAsync(options, firstEventId, secondStudentId));

        Assert.Equal(1, lastSlotResults.Count(result => result.StatusCode == 201));
        Assert.Equal(1, lastSlotResults.Count(result => result.StatusCode == 409));

        var sameStudentResults = await Task.WhenAll(
            RegisterAsync(options, secondEventId, firstStudentId),
            RegisterAsync(options, secondEventId, firstStudentId));

        Assert.Equal(1, sameStudentResults.Count(result => result.StatusCode == 201));
        Assert.Equal(1, sameStudentResults.Count(result => result.StatusCode == 409));

        await using var verifyContext = new AppDbContext(options);
        Assert.Equal(1, await verifyContext.Registrations.CountAsync(r =>
            r.EventId == firstEventId && r.Status == "confirmado"));
        Assert.Equal(1, await verifyContext.Registrations.CountAsync(r =>
            r.EventId == secondEventId && r.UserId == firstStudentId && r.Status == "confirmado"));
        Assert.Equal(1, await verifyContext.Attendances.CountAsync(a =>
            a.EventId == firstEventId && a.UserId != null));
        Assert.Equal(1, (await verifyContext.Events.SingleAsync(e => e.Id == firstEventId)).EnrolledSlots);
        Assert.Equal(1, (await verifyContext.Events.SingleAsync(e => e.Id == secondEventId)).EnrolledSlots);

        var uniqueIndexes = await verifyContext.Database
            .SqlQuery<string>($"""
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE schemaname = current_schema()
                    AND indexname IN (
                        'IX_registrations_UserId_EventId_Active',
                        'IX_attendances_EventId_UserId'
                    )
                """)
            .ToListAsync();
        Assert.Equal(2, uniqueIndexes.Count);
    }

    private static async Task<int> RegisterAsync(
        DbContextOptions<AppDbContext> options,
        string eventId,
        string userId)
    {
        await using var context = new AppDbContext(options);
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

        var result = await controller.Register(new CreateRegistrationDto { EventId = eventId });
        return result.Result switch
        {
            ObjectResult { StatusCode: 201 } => 201,
            ConflictObjectResult => 409,
            _ => result.Result?.GetType().Name == nameof(ConflictObjectResult) ? 409 : 0
        };
    }

    private static User CreateUser(string id, string email) => new()
    {
        Id = id,
        Name = id,
        Email = email,
        PasswordHash = "not-used",
        Role = "Aluno"
    };

    private static Event CreateEvent(string id, string title, int totalSlots, DateTime startDate) => new()
    {
        Id = id,
        Title = title,
        Description = "Evento para teste PostgreSQL",
        Category = "Palestra",
        Modality = "Presencial",
        StartDate = startDate,
        EndDate = startDate.AddHours(2),
        Workload = "2 horas",
        Location = $"Local {id}",
        TotalSlots = totalSlots,
        Status = "Aberto"
    };
}
