namespace ApiIncidentFastTrack.DTOs.ResolutionTypes;

public class UpdateResolutionTypeRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresPayment { get; set; }
}
