using ApiIncidentFastTrack.DTOs.Users;

namespace ApiIncidentFastTrack.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserResponseDto> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken);
    Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto request, CancellationToken cancellationToken);

    // Admin (own company, including themselves) or SuperAdmin sets a new password. Users never reset their own.
    Task ResetPasswordAsync(int userId, ResetUserPasswordRequestDto request, CancellationToken cancellationToken);

    // Soft delete (IsDeleted/DeletedAt). The user can no longer log in and is hidden from lists.
    Task DeleteUserAsync(int userId, CancellationToken cancellationToken);
}
