using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Stores;

public class CreateStoreRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    // Only accepted from a SuperAdmin, who has no company of their own.
    // Any other caller sending it gets 403; their company comes from the JWT.
    [Range(1, int.MaxValue)]
    public int? CompanyId { get; set; }
}
