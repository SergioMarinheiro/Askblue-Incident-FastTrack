using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// Creates a draft (status New). Only CustomerName is required at this point;
// everything else must be filled in before the incident can be submitted.
public class CreateIncidentRequestDto
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
