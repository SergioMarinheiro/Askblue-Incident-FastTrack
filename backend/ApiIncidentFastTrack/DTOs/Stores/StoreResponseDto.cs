namespace ApiIncidentFastTrack.DTOs.Stores;

public class StoreResponseDto
{
    public int StoreId { get; set; }
    public int CompanyId { get; set; }

    public string Name { get; set; }
    public string? Address { get; set; }
}
