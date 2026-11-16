using FirstStep.DTOs.Auth;
using FirstStep.Interfaces;

namespace FirstStep.Services;

public class AuthService : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        return await Task.FromResult(new LoginResponseDto
        {
            Token = "test-token",
            Name = "Test User",
            Role = "Admin",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
    }

    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        return await Task.FromResult("Forgot password endpoint working.");
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return await Task.FromResult("Reset password endpoint working.");
    }
}