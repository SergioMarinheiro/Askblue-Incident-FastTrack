namespace ApiIncidentFastTrack.DTOs.Stores;

public class CreateStoreRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    // Only accepted from a SuperAdmin, who has no company of their own.
    // Any other caller sending it gets 403; their company comes from the JWT.
    public int? CompanyId { get; set; }
}
