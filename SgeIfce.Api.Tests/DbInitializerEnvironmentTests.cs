using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.Models;
using Xunit;

namespace SgeIfce.Api.Tests;

public class DbInitializerEnvironmentTests
{
    [Fact]
    public async Task InitializeAsync_OutsideDevelopment_DoesNotSeedOrModifyExistingUsers()
    {
        await using var context = CreateContext();
        var legacyUser = new User
        {
            Id = "legacy-user",
            Name = "Conta legada",
            Email = "luzia.fernandes@aluno.ifce.edu.br",
            PasswordHash = "123456",
            Role = "Aluno"
        };
        context.Users.Add(legacyUser);
        await context.SaveChangesAsync();

        await DbInitializer.InitializeAsync(context, isDevelopment: false);

        Assert.Equal("123456", legacyUser.PasswordHash);
        Assert.Single(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_InDevelopment_SeedsDemoData()
    {
        await using var context = CreateContext();

        await DbInitializer.InitializeAsync(context, isDevelopment: true);

        Assert.Contains(await context.Users.ToListAsync(), user => user.Role == "Professor");
        Assert.NotEmpty(await context.Events.ToListAsync());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
