using ApiIncidentFastTrack.DTOs.Auth;

namespace ApiIncidentFastTrack.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    Task<string> ForgotPasswordAsync(ForgotPasswordRequestDto request);

    Task<string> ResetPasswordAsync(ResetPasswordRequestDto request);
}