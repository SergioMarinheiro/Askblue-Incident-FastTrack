namespace ApiIncidentFastTrack.DTOs.Stores;

public class LocationResponseDto
{
    public int LocationId { get; set; }
    public int StoreId { get; set; }

    public string Name { get; set; } = string.Empty;
}
