using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Models;

public class User
{
    public int UserId { get; set; }

    public int CompanyId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Company? Company { get; set; }

    public ICollection<Incident> CreatedIncidents { get; set; } = new List<Incident>();
    public ICollection<Incident> ClosedIncidents { get; set; } = new List<Incident>();

    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
    public ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
    public ICollection<IncidentHistory> IncidentHistoryEntries { get; set; } = new List<IncidentHistory>();

    public ICollection<IncidentDuplicate> ReviewedDuplicates { get; set; } = new List<IncidentDuplicate>();
}