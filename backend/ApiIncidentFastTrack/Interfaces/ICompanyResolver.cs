namespace ApiIncidentFastTrack.Interfaces;

// Decides which company a newly created record belongs to (the SuperAdmin exception, see CLAUDE.md).
// Used by every service that creates company-owned data (stores, resolution types, incidents).
public interface ICompanyResolver
{
    // requestedCompanyId: the optional CompanyId from the request DTO.
    // createdThing: used in the error message, e.g. "a store" -> "...when a SuperAdmin creates a store."
    Task<int> ResolveForCreateAsync(int? requestedCompanyId, string createdThing, CancellationToken cancellationToken);
}
