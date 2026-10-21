using AccessRequestSystem.Data.Entities;

namespace AccessRequestSystem.Api.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    int GetExpiryMinutes();
}
