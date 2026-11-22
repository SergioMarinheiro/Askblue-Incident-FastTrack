using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Incidents;

public class ConfirmDuplicateRequestDto
{
    // Required (decided 2026-11-22): why this is a duplicate, e.g. "Same fall as incident 12."
    // Saved as an IncidentComment in the same save as the status change.
    // Length matches IncidentComment.Content.
    [Required]
    [MaxLength(4000)]
    public string Comment { get; set; } = string.Empty;
}
