namespace ApiIncidentFastTrack.DTOs.ResolutionTypes;

public class ResolutionTypeResponseDto
{
    public int ResolutionTypeId { get; set; }
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresPayment { get; set; }
}
