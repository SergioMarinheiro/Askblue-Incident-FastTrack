using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.ResolutionTypes;

public class UpdateResolutionTypeRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool RequiresPayment { get; set; }
}
