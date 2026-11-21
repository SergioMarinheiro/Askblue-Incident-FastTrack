using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.ResolutionTypes;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Tenant isolation:
// - ResolutionType has the "Tenant" query filter, so every query only sees the caller's
//   company (SuperAdmin: all). Another company's resolution type is "not found" (404).
// - On create, CompanyId comes from the JWT (SuperAdmin: from the request, see below).
// Role gates (read: everyone; create/update: AdminOrAbove) are on the controller.
public class ResolutionTypeService : IResolutionTypeService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResolutionTypeService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<ResolutionTypeResponseDto>> GetResolutionTypesAsync(CancellationToken cancellationToken)
    {
        return await _db.ResolutionTypes
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new ResolutionTypeResponseDto
            {
                ResolutionTypeId = r.Id,
                CompanyId = r.CompanyId,
                Name = r.Name,
                Description = r.Description,
                RequiresPayment = r.RequiresPayment
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ResolutionTypeResponseDto> CreateResolutionTypeAsync(CreateResolutionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var companyId = await ResolveCompanyIdAsync(request.CompanyId, cancellationToken);

        var resolutionType = new ResolutionType
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            RequiresPayment = request.RequiresPayment
        };

        _db.ResolutionTypes.Add(resolutionType);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(resolutionType);
    }

    // CompanyId is not editable: moving a resolution type to another company would
    // leave incidents of the old company pointing at it.
    // Changing RequiresPayment does not move incidents that were already approved:
    // their status was decided at approval time.
    public async Task<ResolutionTypeResponseDto> UpdateResolutionTypeAsync(int id, UpdateResolutionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var resolutionType = await _db.ResolutionTypes
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Resolution type {id} was not found.");

        resolutionType.Name = request.Name.Trim();
        resolutionType.Description = NormalizeDescription(request.Description);
        resolutionType.RequiresPayment = request.RequiresPayment;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(resolutionType);
    }

    // Which company a new resolution type belongs to (the SuperAdmin exception, see CLAUDE.md):
    // - Admin: always their own company, from the JWT. Sending CompanyId is rejected (403).
    // - SuperAdmin: has no company, so must say which one. It must exist.
    // Same rules as StoreService. If UserService needs them too, extract a shared helper.
    private async Task<int> ResolveCompanyIdAsync(int? requestedCompanyId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
        {
            if (requestedCompanyId is not null)
            {
                throw new ForbiddenException("Only a SuperAdmin may choose the company. Leave CompanyId out.");
            }

            return _currentUser.CompanyId
                ?? throw new InvalidOperationException("The authenticated user has no company claim.");
        }

        if (requestedCompanyId is not int companyId)
        {
            throw new BusinessRuleException("CompanyId is required when a SuperAdmin creates a resolution type.");
        }

        if (!await _db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken))
        {
            throw new BusinessRuleException($"Company {companyId} does not exist.");
        }

        return companyId;
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static ResolutionTypeResponseDto ToDto(ResolutionType resolutionType) => new()
    {
        ResolutionTypeId = resolutionType.Id,
        CompanyId = resolutionType.CompanyId,
        Name = resolutionType.Name,
        Description = resolutionType.Description,
        RequiresPayment = resolutionType.RequiresPayment
    };
}
