using ApiIncidentFastTrack.DTOs.Auth;

namespace ApiIncidentFastTrack.Interfaces;

public interface IAuthService
{
    // Returns null when the email/password pair is not valid.
    // The caller must not say which of the two was wrong.
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
