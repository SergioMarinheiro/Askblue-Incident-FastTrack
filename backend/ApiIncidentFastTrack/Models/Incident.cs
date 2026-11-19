using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Models;

public class Incident : BaseDomainModel
{
    // Relationships
    public int CompanyId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public int? StoreId { get; set; }
    public int? LocationId { get; set; }
    public int? ResolutionTypeId { get; set; }

    // Incident information
    // Draft rule: only CustomerName is required on create; the rest is required on submit
    // (except EstimatedCompensationAmount, always optional). Enforced in the service.
    public string? Title { get; set; }
    public string? Description { get; set; }

    public IncidentPriority? Priority { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentType? IncidentType { get; set; }

    public DateTime? IncidentDate { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Set only when a Manager/Admin rejects the incident (required at that moment, enforced in the service)
    public string? RejectionReason { get; set; }

    // Customer information
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Compensation
    public decimal? EstimatedCompensationAmount { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? ClosedByUser { get; set; }
    public Store? Store { get; set; }
    public Location? Location { get; set; }
    public ResolutionType? ResolutionType { get; set; }

    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
    public ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
    public ICollection<IncidentHistory> History { get; set; } = new List<IncidentHistory>();

    // Both sides of potential duplicate relationships
    public ICollection<IncidentDuplicate> DuplicateRelationships { get; set; } = new List<IncidentDuplicate>();
    public ICollection<IncidentDuplicate> PotentialDuplicateOfRelationships { get; set; } = new List<IncidentDuplicate>();
}