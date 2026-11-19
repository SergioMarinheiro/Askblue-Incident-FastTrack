using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Models;

public class Store : BaseDomainModel, ISoftDeletable
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}