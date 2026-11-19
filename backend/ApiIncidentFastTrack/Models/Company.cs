namespace ApiIncidentFastTrack.Models;

public class Company
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}