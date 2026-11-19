using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequestDto request)
    {
        var incident = await _incidentService.CreateIncidentAsync(request);

        return CreatedAtAction(
            nameof(GetIncidentById),
            new { id = incident.IncidentId },
            incident
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetAllIncidents()
    {
        var incidents = await _incidentService.GetAllIncidentsAsync();

        return Ok(incidents);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetIncidentById(int id)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id);

        if (incident == null)
            return NotFound();

        return Ok(incident);
    }
}