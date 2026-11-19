using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// Saves changes to an incident. Never changes the status.
// Who may edit, and in which status, is decided in the service.
public class UpdateIncidentRequestDto
{
    public string CustomerName { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? Description { get; set; }

    public IncidentPriority? Priority { get; set; }
    public IncidentType? IncidentType { get; set; }
    public DateTime? IncidentDate { get; set; }

    public int? StoreId { get; set; }
    public int? LocationId { get; set; }
    public int? ResolutionTypeId { get; set; }

    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal? EstimatedCompensationAmount { get; set; }
}
