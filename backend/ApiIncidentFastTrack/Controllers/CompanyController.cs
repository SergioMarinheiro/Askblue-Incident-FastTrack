using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.DTOs.Companies;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Client companies (tenants). Every endpoint is SuperAdmin only: other roles get 403.
[ApiController]
[Route("api/companies")]
[Authorize(Policy = AuthPolicies.SuperAdminOnly)]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var companies = await _companyService.GetCompaniesAsync(cancellationToken);

        return Ok(companies);
    }

    [HttpGet("{companyId:int}")]
    public async Task<IActionResult> GetCompanyById(int companyId, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetCompanyByIdAsync(companyId, cancellationToken);

        return Ok(company);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequestDto request, CancellationToken cancellationToken)
    {
        var company = await _companyService.CreateCompanyAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetCompanyById),
            new { companyId = company.CompanyId },
            company
        );
    }

    // Hard delete, only allowed for a company with no data (409 otherwise).
    [HttpDelete("{companyId:int}")]
    public async Task<IActionResult> DeleteCompany(int companyId, CancellationToken cancellationToken)
    {
        await _companyService.DeleteCompanyAsync(companyId, cancellationToken);

        return NoContent();
    }
}
