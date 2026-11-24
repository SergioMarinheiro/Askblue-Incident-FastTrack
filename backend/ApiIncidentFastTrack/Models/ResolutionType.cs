namespace ApiIncidentFastTrack.Models;

public class ResolutionType : BaseDomainModel
{
    // Per company: each company manages its own list
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    // On approval: true -> AwaitingPayment, false -> AwaitingResolution
    public bool RequiresPayment { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}