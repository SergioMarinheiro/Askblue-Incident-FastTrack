using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiIncidentFastTrack.DTOs.Auth;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Controllers;


// ==========================================
// 1. AUTHENTICATION CONTROLLER (/api/auth)
// ==========================================
[ApiController]
[Route("api/[controller]")] // Base route: /api/auth
// The only endpoints open without a token (everything else falls under the fallback policy in Program.cs).
[AllowAnonymous]

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType<LoginResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (result is null)
        {
            // Same message for unknown email and wrong password.
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid email or password.");
        }

        return Ok(result);
    }

    // Password reset is not decided yet (open question E2): how the email is sent.
    // The routes stay so the contract is visible, but return 501 until then.

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        return Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Password reset is not available yet.");
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public IActionResult ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        return Problem(
            statusCode: StatusCodes.Status501NotImplemented,
            title: "Password reset is not available yet.");
    }
}
