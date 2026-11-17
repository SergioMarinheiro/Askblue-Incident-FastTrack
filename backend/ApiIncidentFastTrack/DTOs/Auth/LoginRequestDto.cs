namespace ApiIncidentFastTrack.DTOs.Auth;

public class LoginRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}