using System.Security.Claims;
using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Reads the caller from HttpContext.User, which the JwtBearer middleware filled in
// after validating the token. Registered as scoped: one instance per request.
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Null outside a request (the seeder at startup, dotnet ef at design time).
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId =>
        int.TryParse(FindClaim(AppClaimTypes.UserId), out var userId)
            ? userId
            : throw new InvalidOperationException("There is no authenticated user with a valid 'sub' claim.");

    public UserRole Role =>
        TryGetRole(out var role)
            ? role
            : throw new InvalidOperationException("There is no authenticated user with a valid 'role' claim.");

    public int? CompanyId =>
        int.TryParse(FindClaim(AppClaimTypes.CompanyId), out var companyId) ? companyId : null;

    public bool IsSuperAdmin => TryGetRole(out var role) && role == UserRole.SuperAdmin;

    // Claims are only trusted from an authenticated identity, so an unauthenticated
    // principal can never look like a SuperAdmin.
    private string? FindClaim(string type) =>
        IsAuthenticated ? Principal!.FindFirst(type)?.Value : null;

    private bool TryGetRole(out UserRole role)
    {
        var value = FindClaim(AppClaimTypes.Role);

        // Enum.TryParse also accepts numbers ("1" would be SuperAdmin), so require the exact name,
        // which is what TokenService writes (user.Role.ToString()).
        return Enum.TryParse(value, out role) && role.ToString() == value;
    }
}
