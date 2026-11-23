using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIncidentFastTrack.Controllers;

// List/get: Manager, Admin (own company), SuperAdmin.
// Create/update/reset password: Admin (own company), SuperAdmin.
// Delete (soft): Manager (Staff only), Admin (any user in own company), SuperAdmin (any user).
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{userId:int}")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> GetUserById(int userId, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateUserAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetUserById),
            new { userId = user.UserId },
            user
        );
    }

    [HttpPut("{userId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateUserAsync(userId, request, cancellationToken);

        return Ok(user);
    }

    // Users never reset their own password: an Admin (own company) or SuperAdmin does it for them.
    [HttpPut("{userId:int}/password")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(int userId, [FromBody] ResetUserPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(userId, request, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{userId:int}")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsync(userId, cancellationToken);

        return NoContent();
    }
}
