using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Companies;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Company is the tenant itself, so it has no tenant filter: only a SuperAdmin reaches
// this service (CompanyController uses the SuperAdminOnly policy), and a SuperAdmin
// sees every company anyway.
public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CompanyResponseDto>> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        return await _db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponseDto
            {
                CompanyId = c.Id,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyResponseDto> GetCompanyByIdAsync(int companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException($"Company {companyId} was not found.");

        return ToDto(company);
    }

    public async Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyRequestDto request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            Name = request.Name.Trim()
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(company);
    }

    // Hard delete, only for an empty company (decided 2026-11-21). Incidents are never deleted,
    // so a company that has any data must stay.
    // "Empty" counts soft-deleted users and stores too: their rows still reference the company,
    // and the foreign keys (DeleteBehavior.Restrict) would block the delete anyway.
    public async Task DeleteCompanyAsync(int companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies
            .SingleOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new NotFoundException($"Company {companyId} was not found.");

        // The caller is a SuperAdmin, whom the tenant filter already lets see everything.
        // IgnoreQueryFilters makes it explicit that these checks must count every row.
        var hasData =
            await _db.Users.AnyAsync(u => u.CompanyId == companyId, cancellationToken)
            || await _db.Stores.IgnoreQueryFilters([AppDbContext.TenantFilter]).AnyAsync(s => s.CompanyId == companyId, cancellationToken)
            || await _db.Incidents.IgnoreQueryFilters([AppDbContext.TenantFilter]).AnyAsync(i => i.CompanyId == companyId, cancellationToken)
            || await _db.ResolutionTypes.IgnoreQueryFilters([AppDbContext.TenantFilter]).AnyAsync(r => r.CompanyId == companyId, cancellationToken);

        if (hasData)
        {
            throw new ConflictException(
                $"Company {companyId} can't be deleted because it has users, stores, incidents or resolution types.");
        }

        _db.Companies.Remove(company);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Data was added to the company between the check above and the delete:
            // the foreign key refused it. Same answer as the check, not a 500.
            throw new ConflictException(
                $"Company {companyId} can't be deleted because it has users, stores, incidents or resolution types.");
        }
    }

    private static CompanyResponseDto ToDto(Company company) => new()
    {
        CompanyId = company.Id,
        Name = company.Name
    };
}
