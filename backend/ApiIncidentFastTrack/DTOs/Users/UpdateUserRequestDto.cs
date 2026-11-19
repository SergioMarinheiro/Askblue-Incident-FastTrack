using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

public class UpdateUserRequestDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }

    // Same assignment rules as on create.
    public UserRole? Role { get; set; }
}
