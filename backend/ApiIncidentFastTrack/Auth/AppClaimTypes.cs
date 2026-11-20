namespace ApiIncidentFastTrack.Auth;

// Claim names written into the JWT. Short JWT-style names, not the long
// "http://schemas.xmlsoap.org/..." URIs (inbound claim mapping is turned off in Program.cs).
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";

    // Absent for SuperAdmin, who has no company.
    public const string CompanyId = "company_id";
}
