using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Interfaces;

// The caller of the current request, read from the JWT claims.
// Services use this instead of HttpContext, and tests can pass in a fake.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    // Throw InvalidOperationException when there is no authenticated user.
    // Every endpoint except login requires a token, so inside a service that means a bug.
    int UserId { get; }
    UserRole Role { get; }

    // Never throw: the tenant query filter reads them on every query,
    // also when nobody is logged in (login, the seeder, dotnet ef).
    // CompanyId is null for SuperAdmin and when nobody is logged in.
    int? CompanyId { get; }
    bool IsSuperAdmin { get; }
}
