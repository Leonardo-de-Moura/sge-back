using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public PasswordResetService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> CreateTokenAsync(User user)
    {
        var now = DateTime.UtcNow;
        var expirationMinutes = GetExpirationMinutes();

        var activeTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.ExpiresAt = now.AddMinutes(-1);
        }

        var tokenValue = CreateSecureToken();

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = HashToken(tokenValue),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(expirationMinutes),
            UsedAt = null
        });

        await _context.SaveChangesAsync();

        return tokenValue;
    }

    public async Task<(User? User, PasswordResetToken? Token)> ValidateTokenAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return (null, null);
        }

        var tokenHash = HashToken(rawToken);

        var token = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token == null)
        {
            return (null, null);
        }

        if (token.UsedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow)
        {
            return (token.User, token);
        }

        return (token.User, token);
    }

    public async Task InvalidateUserTokensAsync(string userId)
    {
        var activeTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.UsedAt = DateTime.UtcNow;
        }

        if (activeTokens.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private int GetExpirationMinutes()
    {
        return int.TryParse(_configuration["PasswordReset:TokenExpirationMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 30;
    }

    private static string CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
