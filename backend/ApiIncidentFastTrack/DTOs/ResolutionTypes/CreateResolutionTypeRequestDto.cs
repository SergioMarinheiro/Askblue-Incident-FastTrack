using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.ResolutionTypes;

public class CreateResolutionTypeRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // true: approval moves the incident to AwaitingPayment. false: AwaitingResolution.
    public bool RequiresPayment { get; set; }

    // Only accepted from a SuperAdmin. Any other caller sending it gets 403.
    [Range(1, int.MaxValue)]
    public int? CompanyId { get; set; }
}
