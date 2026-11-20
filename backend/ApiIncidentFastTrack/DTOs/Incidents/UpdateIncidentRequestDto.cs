using System.ComponentModel.DataAnnotations;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// Saves changes to an incident. Never changes the status.
// Who may edit, and in which status, is decided in the service.
// Lengths match Data/Configurations/IncidentConfiguration.cs.
public class UpdateIncidentRequestDto
{
    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    // JSON accepts any number for an enum; these reject values that aren't defined.
    [EnumDataType(typeof(IncidentPriority))]
    public IncidentPriority? Priority { get; set; }

    [EnumDataType(typeof(IncidentType))]
    public IncidentType? IncidentType { get; set; }

    public DateTime? IncidentDate { get; set; }

    // Whether the store, location and resolution type exist and belong to the
    // caller's company is checked in the service.
    [Range(1, int.MaxValue)]
    public int? StoreId { get; set; }

    [Range(1, int.MaxValue)]
    public int? LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ResolutionTypeId { get; set; }

    [MaxLength(500)]
    public string? CustomerAddress { get; set; }

    [EmailAddress]
    [MaxLength(256)]
    public string? CustomerEmail { get; set; }

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    // decimal(18,2): at most 16 digits before the point. Never negative.
    // Invariant culture: on a pt-PT machine "9999999999999999.99" would otherwise fail to parse.
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? EstimatedCompensationAmount { get; set; }
}
