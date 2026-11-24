using System.ComponentModel.DataAnnotations;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Incidents;

// Creates a draft (status New). Only CustomerName is required at this point;
// everything else must be filled in before the incident can be submitted
// (checked by the service at submit, not here).
// Lengths match Data/Configurations/IncidentConfiguration.cs.
public class CreateIncidentRequestDto
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

    // Only accepted from a SuperAdmin, who has no company of their own (required for them).
    // Any other caller sending it gets 403; their company comes from the JWT.
    [Range(1, int.MaxValue)]
    public int? CompanyId { get; set; }
}
