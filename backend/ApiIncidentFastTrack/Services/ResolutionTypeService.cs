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
// - On create, the company comes from ICompanyResolver (JWT; SuperAdmin: from the request).
// Role gates (read: everyone; create/update: AdminOrAbove) are on the controller.
public class ResolutionTypeService : IResolutionTypeService
{
    private readonly AppDbContext _db;
    private readonly ICompanyResolver _companyResolver;

    public ResolutionTypeService(AppDbContext db, ICompanyResolver companyResolver)
    {
        _db = db;
        _companyResolver = companyResolver;
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
                RequiresPayment = r.RequiresPayment
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ResolutionTypeResponseDto> CreateResolutionTypeAsync(CreateResolutionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var companyId = await _companyResolver.ResolveForCreateAsync(request.CompanyId, "a resolution type", cancellationToken);

        var resolutionType = new ResolutionType
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
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
        resolutionType.RequiresPayment = request.RequiresPayment;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(resolutionType);
    }

    private static ResolutionTypeResponseDto ToDto(ResolutionType resolutionType) => new()
    {
        ResolutionTypeId = resolutionType.Id,
        CompanyId = resolutionType.CompanyId,
        Name = resolutionType.Name,
        RequiresPayment = resolutionType.RequiresPayment
    };
}
