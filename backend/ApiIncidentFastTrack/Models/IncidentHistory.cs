namespace ApiIncidentFastTrack.Models;

// CreatedAt (from the base) is when the change was made.
public class IncidentHistory : BaseDomainModel
{
    public int IncidentId { get; set; }
    public int ChangedByUserId { get; set; }

    public Guid ChangeSetId { get; set; } //Unique identifier for a set of changes made together

    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}