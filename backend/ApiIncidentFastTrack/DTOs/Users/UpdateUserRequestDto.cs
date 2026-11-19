using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

public class UpdateUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Same assignment rules as on create.
    public UserRole Role { get; set; }
}
