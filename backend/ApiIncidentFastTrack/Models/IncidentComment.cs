namespace ApiIncidentFastTrack.Models;

public class IncidentComment : BaseDomainModel
{
    public int IncidentId { get; set; }
    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public User User { get; set; } = null!;
}