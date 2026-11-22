using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// Any authenticated user may call an action without an [Authorize] attribute (fallback policy).
// The attributes are the coarse role gate. Rules such as "own incident only" or
// "only in status X" are enforced in IncidentService.
[ApiController]
[Route("api/incidents")]
public class IncidentController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    // Staff: own incidents. Manager/Admin: their company. SuperAdmin: all.
    [HttpGet]
    public async Task<IActionResult> GetAllIncidents(CancellationToken cancellationToken)
    {
        var incidents = await _incidentService.GetAllIncidentsAsync(cancellationToken);

        return Ok(incidents);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetIncidentById(int id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id, cancellationToken);

        return Ok(incident);
    }

    // Anyone who can see the incident: every recorded edit, oldest first.
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetIncidentHistory(int id, CancellationToken cancellationToken)
    {
        var history = await _incidentService.GetIncidentHistoryAsync(id, cancellationToken);

        return Ok(history);
    }

    // Any role: creates a draft (status New). Only CustomerName is required.
    [HttpPost]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.CreateIncidentAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetIncidentById),
            new { id = incident.IncidentId },
            incident
        );
    }

    // Saves changes; never changes the status. Who may edit depends on role and status.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateIncident(int id, [FromBody] UpdateIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.UpdateIncidentAsync(id, request, cancellationToken);

        return Ok(incident);
    }

    // The incident's creator (New only): hard delete after acknowledging a duplicate found at submit.
    // This is the only way an incident is ever deleted.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteDuplicateIncident(int id, CancellationToken cancellationToken)
    {
        await _incidentService.DeleteDuplicateIncidentAsync(id, cancellationToken);

        return NoContent();
    }

    // The incident's creator: New/SentBack -> AwaitingApproval.
    // 200 when submitted; 409 with the possible duplicates when matches are found.
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> SubmitIncident(int id, [FromBody] SubmitIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _incidentService.SubmitIncidentAsync(id, request, cancellationToken);

        if (!result.Submitted)
            return Conflict(result.PossibleDuplicates);

        return Ok(result.Incident);
    }

    // Manager/Admin: AwaitingApproval -> AwaitingPayment or AwaitingResolution (from ResolutionType.RequiresPayment).
    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> ApproveIncident(int id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.ApproveIncidentAsync(id, cancellationToken);

        return Ok(incident);
    }

    // Manager/Admin: AwaitingApproval -> Rejected. A reason is required.
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> RejectIncident(int id, [FromBody] RejectIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.RejectIncidentAsync(id, request, cancellationToken);

        return Ok(incident);
    }

    // Manager/Admin: AwaitingApproval -> SentBack. What to fix is required, saved as a comment.
    [HttpPost("{id:int}/send-back")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> SendBackIncident(int id, [FromBody] SendBackIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.SendBackIncidentAsync(id, request, cancellationToken);

        return Ok(incident);
    }

    // Staff/Manager/Admin: AwaitingPayment/AwaitingResolution -> Completed.
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> CompleteIncident(int id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.CompleteIncidentAsync(id, cancellationToken);

        return Ok(incident);
    }

    // Staff (own)/Manager/Admin: any open status -> Canceled.
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelIncident(int id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.CancelIncidentAsync(id, cancellationToken);

        return Ok(incident);
    }

    // The duplicate flags saved when Staff proceeded despite possible duplicates.
    [HttpGet("{id:int}/duplicates")]
    public async Task<IActionResult> GetDuplicateFlags(int id, CancellationToken cancellationToken)
    {
        var flags = await _incidentService.GetDuplicateFlagsAsync(id, cancellationToken);

        return Ok(flags);
    }

    // Manager/Admin: Pending flags -> NotDuplicate; the normal flow continues.
    [HttpPost("{id:int}/duplicates/dismiss")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> DismissDuplicateFlags(int id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.DismissDuplicateFlagsAsync(id, cancellationToken);

        return Ok(incident);
    }

    // Manager/Admin: -> Duplicate (closed). The explanation is required, saved as a comment.
    [HttpPost("{id:int}/duplicates/confirm")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> ConfirmDuplicate(int id, [FromBody] ConfirmDuplicateRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.ConfirmDuplicateAsync(id, request, cancellationToken);

        return Ok(incident);
    }
}
