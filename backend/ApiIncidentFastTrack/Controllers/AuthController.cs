using Microsoft.AspNetCore.Mvc;
using ApiIncidentFastTrack.DTOs.Auth;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Services;

namespace ApiIncidentFastTrack.Controllers;


// ==========================================
// 1. AUTHENTICATION CONTROLLER (/api/auth)
// ==========================================
[ApiController]
[Route("api/[controller]")] // Base route: /api/auth

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        return Ok(result);
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        return Ok(result);
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return Ok(result);
    }
}