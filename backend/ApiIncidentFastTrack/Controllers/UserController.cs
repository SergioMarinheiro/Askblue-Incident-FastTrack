using ApiIncidentFastTrack.Auth;
using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiIncidentFastTrack.Controllers;

// List/get: Manager, Admin (own company), SuperAdmin.
// Create/update: Admin (own company), SuperAdmin.
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

        if(users.IsNullOrEmpty())
        {
            return NotFound();
        }

        return Ok(users);
    }

    [HttpGet("{userId:int}")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> GetUserById(int userId, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);

        if(user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = user.UserId },
                user
            );
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPut("{userId:int}")]
    [Authorize(Policy = AuthPolicies.AdminOrAbove)]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(userId, request, cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{userId:int}")]
    [Authorize(Policy = AuthPolicies.ManagerOrAbove)]
    public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteUserAsync(userId, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
