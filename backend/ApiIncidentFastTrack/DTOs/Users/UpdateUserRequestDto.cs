using System.ComponentModel.DataAnnotations;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

public class UpdateUserRequestDto
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

    // Same assignment rules as on create.
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }
}
