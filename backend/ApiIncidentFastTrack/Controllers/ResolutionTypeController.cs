using ApiIncidentFastTrack.DTOs.ResolutionTypes;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Read: anyone in the company (to pick an outcome on an incident).
// Create/update: Admin (own company) and SuperAdmin.
[ApiController]
[Route("api/resolution-types")]
public class ResolutionTypeController : ControllerBase
{
    private readonly IResolutionTypeService _resolutionTypeService;

    public ResolutionTypeController(IResolutionTypeService resolutionTypeService)
    {
        _resolutionTypeService = resolutionTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetResolutionTypes(CancellationToken cancellationToken)
    {
        var resolutionTypes = await _resolutionTypeService.GetResolutionTypesAsync(cancellationToken);

        return Ok(resolutionTypes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateResolutionType([FromBody] CreateResolutionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var resolutionType = await _resolutionTypeService.CreateResolutionTypeAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetResolutionTypes),
            null,
            resolutionType
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateResolutionType(int id, [FromBody] UpdateResolutionTypeRequestDto request, CancellationToken cancellationToken)
    {
        var resolutionType = await _resolutionTypeService.UpdateResolutionTypeAsync(id, request, cancellationToken);

        return Ok(resolutionType);
    }
}
