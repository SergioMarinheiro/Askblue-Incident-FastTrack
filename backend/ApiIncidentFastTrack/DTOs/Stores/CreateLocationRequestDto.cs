namespace ApiIncidentFastTrack.DTOs.Stores;

// The store (and therefore the company) comes from the route, not the body.
public class CreateLocationRequestDto
{
    public required string Name { get; set; }
}
