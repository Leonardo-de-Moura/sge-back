using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public interface IPasswordResetService
{
    Task<string> CreateTokenAsync(User user);
    Task<(User? User, PasswordResetToken? Token)> ValidateTokenAsync(string rawToken);
    Task InvalidateUserTokensAsync(string userId);
}
