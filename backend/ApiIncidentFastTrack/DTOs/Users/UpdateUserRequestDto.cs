using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

// Activation is changed through the deactivate/activate endpoints, not here.
public class UpdateUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Same assignment rules as on create.
    public UserRole Role { get; set; }
}
