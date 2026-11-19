using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// An existing open incident that matched the duplicate check on submit.
public class PossibleDuplicateDto
{
    public int IncidentId { get; set; }
    public string? Title { get; set; }
    public IncidentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Score { get; set; }
}
