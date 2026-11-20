using System.Text;
using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Models;
using ApiIncidentFastTrack.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiIncidentFastTrack.Tests.Services;

public class TokenServiceTests
{
    private const string SigningKey = "test-signing-key-that-is-at-least-32-bytes-long";
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = SigningKey,
        ExpiryMinutes = 60
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static TokenService CreateService(DateTimeOffset now) =>
        new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(now));

    private static User CreateUser(UserRole role, int? companyId) => new()
    {
        Id = 42,
        CompanyId = companyId,
        Email = "manager@example.com",
        FirstName = "Test",
        LastName = "User",
        Role = role
    };

    // Validates exactly as Program.cs does, but with lifetime checked against the fixed clock.
    private static async Task<TokenValidationResult> ValidateAsync(string token, string signingKey, DateTimeOffset now)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (notBefore, expires, _, _) => notBefore <= now.UtcDateTime && now.UtcDateTime < expires
        };

        return await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    [Fact]
    public async Task CreateToken_ForCompanyUser_ContainsUserIdEmailRoleAndCompanyId()
    {
        var (token, _) = CreateService(Now).CreateToken(CreateUser(UserRole.Manager, companyId: 7));

        var result = await ValidateAsync(token, SigningKey, Now);

        Assert.True(result.IsValid);
        Assert.Equal("42", result.Claims[AppClaimTypes.UserId]);
        Assert.Equal("manager@example.com", result.Claims[AppClaimTypes.Email]);
        Assert.Equal("Manager", result.Claims[AppClaimTypes.Role]);
        Assert.Equal("7", result.Claims[AppClaimTypes.CompanyId]);
    }

    [Fact]
    public async Task CreateToken_ForSuperAdmin_HasNoCompanyIdClaim()
    {
        var (token, _) = CreateService(Now).CreateToken(CreateUser(UserRole.SuperAdmin, companyId: null));

        var result = await ValidateAsync(token, SigningKey, Now);

        Assert.True(result.IsValid);
        Assert.Equal("SuperAdmin", result.Claims[AppClaimTypes.Role]);
        Assert.False(result.Claims.ContainsKey(AppClaimTypes.CompanyId));
    }

    [Fact]
    public async Task CreateToken_ExpiresAfterConfiguredMinutes()
    {
        var (token, expiresAt) = CreateService(Now).CreateToken(CreateUser(UserRole.Staff, companyId: 7));

        Assert.Equal(Now.UtcDateTime.AddMinutes(60), expiresAt);
        Assert.True((await ValidateAsync(token, SigningKey, Now.AddMinutes(59))).IsValid);
        Assert.False((await ValidateAsync(token, SigningKey, Now.AddMinutes(61))).IsValid);
    }

    [Fact]
    public async Task CreateToken_IsRejectedWithADifferentSigningKey()
    {
        var (token, _) = CreateService(Now).CreateToken(CreateUser(UserRole.Admin, companyId: 7));

        var result = await ValidateAsync(token, "another-signing-key-that-is-also-32-bytes-long", Now);

        Assert.False(result.IsValid);
    }
}
