using ApiIncidentFastTrack.DTOs.Users;

namespace ApiIncidentFastTrack.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserResponseDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken);
    Task<UserResponseDto?> UpdateUserAsync(int userId, UpdateUserRequestDto request, CancellationToken cancellationToken);

    // Soft delete (IsDeleted/DeletedAt). The user can no longer log in and is hidden from lists.
    Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken);
}
