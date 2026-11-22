using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;

namespace ApiIncidentFastTrack.Services;

// The one place that decides which incidents a caller may see. Used by every service that
// reads or changes incidents or their children (comments, history, duplicate flags), so the
// rule can't drift between them.
// - Company: the "Tenant" query filter on Incident already limits it (SuperAdmin: all companies).
// - Staff: only incidents they created.
// Anything not visible is reported as 404 by the callers.
public static class IncidentVisibility
{
    public static IQueryable<Incident> VisibleTo(this IQueryable<Incident> incidents, ICurrentUser currentUser)
    {
        if (currentUser.Role == UserRole.Staff)
        {
            var userId = currentUser.UserId;
            return incidents.Where(i => i.CreatedByUserId == userId);
        }

        return incidents;
    }
}
