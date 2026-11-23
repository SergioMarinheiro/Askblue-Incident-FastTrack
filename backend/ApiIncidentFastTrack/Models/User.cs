using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Models;

public class User : BaseDomainModel, ISoftDeletable
{
    // Null only for SuperAdmin (AskBlue platform staff, no company)
    public int? CompanyId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    // Soft delete: a deleted user can't log in and is hidden from lists,
    // but stays referenced by their incidents, comments and history.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public Company? Company { get; set; }

    public ICollection<Incident> CreatedIncidents { get; set; } = new List<Incident>();
    public ICollection<Incident> ClosedIncidents { get; set; } = new List<Incident>();
    public ICollection<Incident> ApprovedIncidents { get; set; } = new List<Incident>();

    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
    public ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
    public ICollection<IncidentHistory> IncidentHistoryEntries { get; set; } = new List<IncidentHistory>();

    public ICollection<IncidentDuplicate> ReviewedDuplicates { get; set; } = new List<IncidentDuplicate>();
}