using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

public class IncidentResponseDto
{
    public int IncidentId { get; set; }
    public int CompanyId { get; set; }

    public IncidentStatus Status { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    public IncidentPriority? Priority { get; set; }
    public IncidentType? IncidentType { get; set; }
    public DateTime? IncidentDate { get; set; }

    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? ResolutionTypeId { get; set; }
    public string? ResolutionTypeName { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    public decimal? EstimatedCompensationAmount { get; set; }

    public string? RejectionReason { get; set; }

    // True while at least one duplicate flag is still pending review
    public bool HasPendingDuplicateFlag { get; set; }

    public int CreatedByUserId { get; set; }
    // For display only ("Reported by"). Logic such as "is this my incident?" uses CreatedByUserId.
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }
}
