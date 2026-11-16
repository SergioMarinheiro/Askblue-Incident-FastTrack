using Microsoft.AspNetCore.Mvc;
using FirstStep.DTOs.Auth;
using FirstStep.Interfaces;
using FirstStep.Services;

namespace FirstStep.Controllers;


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

// // ==========================================
// // 2. USERS CONTROLLER (/api/users)
// // Handles both regular users and admins via the Role property
// // ==========================================
// [ApiController]
// [Route("api/[controller]")]
// public class UsersController : ControllerBase
// {
//     [HttpGet]
//     public ActionResult<IEnumerable<User>> GetUsers()
//     {
//         // Logic to return all users (including admins)
//     }

//     [HttpGet("{id:int}")]
//     public ActionResult<User> GetUserById(int id)
//     {
//         // Logic to return a specific user by ID
//     }

//     [HttpPost]
//     public ActionResult<User> CreateUser([FromBody] User user)
//     {
//         // Logic to create a user (user.Role defines if they are "User" or "Admin")
//     }
// }

// // ==========================================
// // 3. INCIDENTS CONTROLLER (/api/incidents)
// // ==========================================
// [ApiController]
// [Route("api/[controller]")]
// public class IncidentsController : ControllerBase
// {
//     [HttpGet]
//     public ActionResult<IEnumerable<Incident>> GetIncidents()
//     {
//         // Logic to return all incidents
//     }

//     [HttpGet("{id:int}")]
//     public ActionResult<Incident> GetIncidentById(int id)
//     {
//         // Logic to return a specific incident by ID
//     }

//     [HttpPost]
//     public ActionResult<Incident> CreateIncident([FromBody] Incident incident)
//     {
//         // Logic to create a new incident
//     }
// }