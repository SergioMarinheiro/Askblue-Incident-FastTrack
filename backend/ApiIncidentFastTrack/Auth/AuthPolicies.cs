namespace ApiIncidentFastTrack.Auth;

// Named authorization policies, registered in Program.cs.
// Use them as [Authorize(Policy = AuthPolicies.AdminOrAbove)] so role lists
// aren't repeated as strings across controllers.
//
// These are only the coarse gate per endpoint. Finer rules (own incidents only,
// a Manager may delete only Staff, edits depending on status) live in the services.
public static class AuthPolicies
{
    // Manager, Admin, SuperAdmin
    public const string ManagerOrAbove = "ManagerOrAbove";

    // Admin, SuperAdmin
    public const string AdminOrAbove = "AdminOrAbove";
}
