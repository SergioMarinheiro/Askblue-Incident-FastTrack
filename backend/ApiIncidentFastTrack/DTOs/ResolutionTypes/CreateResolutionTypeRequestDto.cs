namespace ApiIncidentFastTrack.DTOs.ResolutionTypes;

public class CreateResolutionTypeRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // true: approval moves the incident to AwaitingPayment. false: AwaitingResolution.
    public bool RequiresPayment { get; set; }

    // Only accepted from a SuperAdmin. Any other caller sending it gets 403.
    public int? CompanyId { get; set; }
}
