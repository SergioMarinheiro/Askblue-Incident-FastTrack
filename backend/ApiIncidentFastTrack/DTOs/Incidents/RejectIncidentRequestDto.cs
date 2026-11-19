namespace ApiIncidentFastTrack.DTOs.Incidents;

public class RejectIncidentRequestDto
{
    // Required. Stored in Incident.RejectionReason.
    public string Reason { get; set; } = string.Empty;
}
