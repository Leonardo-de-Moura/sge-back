using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SgeIfce.Api.Data;
using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public sealed class EmailConfirmationService : IEmailConfirmationService
{
    private readonly AppDbContext _context;

    public EmailConfirmationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var existingTokens = await _context.EmailConfirmationTokens
            .Where(token => token.UserId == user.Id && token.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.UsedAt = now;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _context.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddHours(24)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public Task<EmailConfirmationToken?> FindTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token) || token.Length != 64 || !token.All(Uri.IsHexDigit))
        {
            return Task.FromResult<EmailConfirmationToken?>(null);
        }

        var tokenHash = HashToken(token.ToUpperInvariant());
        return _context.EmailConfirmationTokens
            .Include(confirmationToken => confirmationToken.User)
            .FirstOrDefaultAsync(
                confirmationToken => confirmationToken.TokenHash == tokenHash,
                cancellationToken);
    }

    public Task<bool> HasRecentTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-1);
        return _context.EmailConfirmationTokens.AnyAsync(
            token => token.UserId == userId && token.CreatedAt >= threshold,
            cancellationToken);
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
