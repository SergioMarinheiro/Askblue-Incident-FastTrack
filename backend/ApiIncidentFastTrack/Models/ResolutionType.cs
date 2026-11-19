namespace ApiIncidentFastTrack.Models;

public class ResolutionType
{
    public int ResolutionTypeId { get; set; }

    // Per company: each company manages its own list
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // On approval: true -> AwaitingPayment, false -> AwaitingResolution
    public bool RequiresPayment { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}