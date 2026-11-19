namespace ApiIncidentFastTrack.Models;

public class Store
{
    public int StoreId { get; set; }

    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}