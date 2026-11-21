using ApiIncidentFastTrack.DTOs.Companies;

namespace ApiIncidentFastTrack.Interfaces;

// Client companies (tenants). SuperAdmin only.
public interface ICompanyService
{
    Task<IEnumerable<CompanyResponseDto>> GetCompaniesAsync(CancellationToken cancellationToken);
    Task<CompanyResponseDto> GetCompanyByIdAsync(int companyId, CancellationToken cancellationToken);
    Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyRequestDto request, CancellationToken cancellationToken);
    Task DeleteCompanyAsync(int companyId, CancellationToken cancellationToken);
}
