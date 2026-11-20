using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Incidents;

public class RejectIncidentRequestDto
{
    // Required. Stored in Incident.RejectionReason.
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
