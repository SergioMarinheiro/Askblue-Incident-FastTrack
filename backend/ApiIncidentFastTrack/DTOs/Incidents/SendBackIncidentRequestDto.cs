using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Incidents;

public class SendBackIncidentRequestDto
{
    // Required (decided 2026-11-22): tells the reporter what to fix.
    // Saved as an IncidentComment in the same save as the status change.
    // Length matches IncidentComment.Content.
    [Required]
    [MaxLength(4000)]
    public string Comment { get; set; } = string.Empty;
}
