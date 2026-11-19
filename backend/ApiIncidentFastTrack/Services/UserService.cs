using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Interfaces;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class UserService : IUserService
{
    public Task<IEnumerable<UserResponseDto>> GetUsersAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponseDto> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponseDto> DeactivateUserAsync(int userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponseDto> ActivateUserAsync(int userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
