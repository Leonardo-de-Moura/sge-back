using Microsoft.Extensions.Configuration;
using SgeIfce.Api.Models;
using SgeIfce.Api.Services;
using Xunit;

namespace SgeIfce.Api.Tests;

public class TokenServiceSecurityTests
{
    [Fact]
    public void GenerateToken_WithoutConfiguredSecret_Throws()
    {
        var service = new TokenService(new ConfigurationBuilder().Build());

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(CreateUser()));
    }

    [Fact]
    public void GenerateToken_WithSecretShorterThan32Bytes_Throws()
    {
        var service = new TokenService(CreateConfiguration(new string('k', 31)));

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(CreateUser()));
    }

    [Fact]
    public void GenerateToken_WithConfiguredSecret_Succeeds()
    {
        var service = new TokenService(CreateConfiguration(new string('k', 32)));

        var (token, expiresAt) = service.GenerateToken(CreateUser());

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);
    }

    private static IConfiguration CreateConfiguration(string secretKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = secretKey
            })
            .Build();

    private static User CreateUser() => new()
    {
        Id = "user-1",
        Name = "Teste",
        Email = "teste@ifce.edu.br",
        Role = "Aluno"
    };
}
