namespace ApiIncidentFastTrack.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    // Lets the frontend tell "my incident" apart (e.g. only the creator may submit it).
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}