using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Models;

public class Location : BaseDomainModel, ISoftDeletable
{
    public int StoreId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Store Store { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; }
        = new List<Incident>();
}
