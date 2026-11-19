namespace ApiIncidentFastTrack.Models;

// Common key and audit dates for every entity.
// CreatedAt/ModifiedAt are set automatically in AppDbContext.SaveChanges.
public abstract class BaseDomainModel
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
