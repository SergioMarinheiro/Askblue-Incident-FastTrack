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

    // No self-service password reset (decided 2026-11-23): an Admin or SuperAdmin sets a new
    // password through PUT /api/users/{userId}/password.
}
