using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Users;

// PUT /api/users/{userId}/password. An Admin or SuperAdmin sets a new password for a user.
public class ResetUserPasswordRequestDto
{
    // Stored only as a hash. Password rules (minimum length etc.) are not decided yet.
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
