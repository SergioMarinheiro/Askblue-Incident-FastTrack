namespace ApiIncidentFastTrack.Models;

public class Location
{
    public int LocationId { get; set; }
    public int StoreId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Store Store { get; set; } = null!;

    public ICollection<Incident> Incidents { get; set; }
        = new List<Incident>();
}
