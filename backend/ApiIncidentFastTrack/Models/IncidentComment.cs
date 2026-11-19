namespace ApiIncidentFastTrack.Models;

public class IncidentComment
{
    public int IncidentCommentId { get; set; }

    public int IncidentId { get; set; }
    public int UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public User User { get; set; } = null!;
}