using System.ComponentModel.DataAnnotations;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

// Lengths match Data/Configurations/UserConfiguration.cs.
public class CreateUserRequestDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // Initial password chosen by the creator. Stored only as a hash.
    // Password rules (minimum length etc.) are not decided yet.
    [Required]
    public string Password { get; set; } = string.Empty;

    // An Admin may assign Staff, Manager or Admin. Only a SuperAdmin may assign SuperAdmin.
    // EnumDataType also rejects a missing role, which would arrive as 0 (not a defined role).
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    // Only accepted from a SuperAdmin. Any other caller sending it gets 403.
    [Range(1, int.MaxValue)]
    public int? CompanyId { get; set; }
}
