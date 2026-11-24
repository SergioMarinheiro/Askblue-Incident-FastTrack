using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Rules:
// - Any role except SuperAdmin: always their own company, from the JWT. Sending CompanyId
//   is rejected (403), never silently ignored, so a client bug or a tampering attempt is visible.
// - SuperAdmin: has no company, so must say which one (422 if missing). It must exist (422).
public class CompanyResolver : ICompanyResolver
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CompanyResolver(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> ResolveForCreateAsync(int? requestedCompanyId, string createdThing, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
        {
            if (requestedCompanyId is not null)
            {
                throw new ForbiddenException("Only a SuperAdmin may choose the company. Leave CompanyId out.");
            }

            // Every role except SuperAdmin has a company, so a missing claim is a bug, not a user error (500).
            return _currentUser.CompanyId
                ?? throw new InvalidOperationException("The authenticated user has no company claim.");
        }

        if (requestedCompanyId is not int companyId)
        {
            throw new BusinessRuleException($"CompanyId is required when a SuperAdmin creates {createdThing}.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
        {
            throw new BusinessRuleException($"Company {companyId} does not exist.");
        }

        return companyId;
    }
}
