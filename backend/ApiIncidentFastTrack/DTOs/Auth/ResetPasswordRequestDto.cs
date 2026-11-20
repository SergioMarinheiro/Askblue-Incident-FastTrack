using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    // Password rules (minimum length etc.) are not decided yet.
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
