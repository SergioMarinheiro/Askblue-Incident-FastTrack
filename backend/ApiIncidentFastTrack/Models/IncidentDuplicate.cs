using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Models;

public class IncidentDuplicate
{
    public int IncidentDuplicateId { get; set; }

    public int IncidentId { get; set; }
    public int PotentialDuplicateIncidentId { get; set; }

    public DateTime DetectedAt { get; set; }

    public decimal? DetectionScore { get; set; }

    public DuplicateReviewStatus ReviewStatus { get; set; } = DuplicateReviewStatus.Pending;

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public string? Justification { get; set; }

    // Navigation properties
    public Incident Incident { get; set; } = null!;
    public Incident PotentialDuplicateIncident { get; set; } = null!;

    public User? ReviewedByUser { get; set; }
}