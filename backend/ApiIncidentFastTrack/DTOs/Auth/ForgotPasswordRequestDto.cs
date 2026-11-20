using System.ComponentModel.DataAnnotations;

namespace ApiIncidentFastTrack.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
