using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// A saved duplicate flag, as the Manager sees it when reviewing an incident.
public class IncidentDuplicateResponseDto
{
    public int IncidentDuplicateId { get; set; }

    public int PotentialDuplicateIncidentId { get; set; }
    public string? PotentialDuplicateTitle { get; set; }
    public IncidentStatus PotentialDuplicateStatus { get; set; }

    public decimal? DetectionScore { get; set; }
    public DateTime DetectedAt { get; set; }

    public DuplicateReviewStatus ReviewStatus { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
