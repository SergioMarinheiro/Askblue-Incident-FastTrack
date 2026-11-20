using System.Security.Claims;
using System.Text;
using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiIncidentFastTrack.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Email, user.Email),
            new(AppClaimTypes.Role, user.Role.ToString()),
            // Unique token id, useful later for logging or revocation.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // SuperAdmin has no company, so the claim is simply left out.
        if (user.CompanyId is int companyId)
        {
            claims.Add(new Claim(AppClaimTypes.CompanyId, companyId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return (token, expiresAt);
    }
}
