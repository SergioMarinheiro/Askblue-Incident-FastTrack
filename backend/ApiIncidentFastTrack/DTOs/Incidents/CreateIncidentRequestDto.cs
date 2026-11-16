namespace ApiIncidentFastTrack.DTOs.Incidents;

public class CreateIncidentRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}