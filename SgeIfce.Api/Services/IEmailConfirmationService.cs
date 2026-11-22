using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public interface IEmailConfirmationService
{
    Task<string> CreateTokenAsync(User user, CancellationToken cancellationToken = default);

    Task<EmailConfirmationToken?> FindTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> HasRecentTokenAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
