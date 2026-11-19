using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.DTOs.Users;

public class CreateUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Initial password chosen by the creator. Stored only as a hash.
    public string Password { get; set; } = string.Empty;

    // An Admin may assign Staff, Manager or Admin. Only a SuperAdmin may assign SuperAdmin.
    public UserRole Role { get; set; }

    // Only accepted from a SuperAdmin. Any other caller sending it gets 403.
    public int? CompanyId { get; set; }
}
