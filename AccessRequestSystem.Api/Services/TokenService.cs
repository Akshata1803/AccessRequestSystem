using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccessRequestSystem.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AccessRequestSystem.Api.Services;

/// <summary>
/// Service responsible for signing JWT tokens with user identities and roles.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int GetExpiryMinutes()
    {
        return _configuration.GetValue<int>("Jwt:ExpiryMinutes", 30);
    }

    public string GenerateToken(User user)
    {
        var keyString = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims convey identity and permissions to downstream API filters
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
        };

        var expiry = DateTime.UtcNow.AddMinutes(GetExpiryMinutes());

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiry,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
