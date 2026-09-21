using SgeIfce.Api.Models;

namespace SgeIfce.Api.Services;

public interface ITokenService
{
    (string token, DateTime expiresAt) GenerateToken(User user);
}
