using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Stores;

// The store (and therefore the company) comes from the route, not the body.
public class CreateLocationRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
