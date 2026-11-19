using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Domain;

public class User : BaseDomainModel, ISoftDeletable
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}