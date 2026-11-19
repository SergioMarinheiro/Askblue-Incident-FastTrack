namespace ApiIncidentFastTrack.Models;

public class ResolutionType
{
    public int ResolutionTypeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // On approval: true -> AwaitingPayment, false -> AwaitingResolution
    public bool RequiresPayment { get; set; }

    // Navigation property
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}