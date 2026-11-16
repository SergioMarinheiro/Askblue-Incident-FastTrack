using FirstStep.DTOs.Auth;

namespace FirstStep.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    Task<string> ForgotPasswordAsync(ForgotPasswordRequestDto request);

    Task<string> ResetPasswordAsync(ResetPasswordRequestDto request);
}