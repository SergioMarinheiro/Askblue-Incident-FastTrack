using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ApiIncidentFastTrack.Services;

// Contract only: implemented once the database, auth and tenant plumbing exist.
public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(user => !user.IsDeleted)
            .Select(user => new UserResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(user => user.Id == userId && !user.IsDeleted)
            .Select(user => new UserResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                CompanyId = user.CompanyId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var emailExists = await _context.Users.AnyAsync(
        user => user.Email == request.Email && !user.IsDeleted,
        cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = request.Password,
            Role = request.Role,
            CompanyId = request.CompanyId
        };
          
        await _context.Users.AddAsync(user, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            throw new InvalidOperationException(
                "A user with this email already exists.",
                exception);
        }

        return new UserResponseDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            CompanyId = user.CompanyId
        };
    }

    public async Task<UserResponseDto?> UpdateUserAsync(int userId, UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var email = request.Email ?? user.Email;
        var emailExists = await _context.Users.AnyAsync(
            existingUser =>
                existingUser.Email == email &&
                existingUser.Id != userId &&
                !existingUser.IsDeleted,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.Email = email;
        user.Role = request.Role ?? user.Role;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            throw new InvalidOperationException(
                "A user with this email already exists.",
                exception);
        }

        return new UserResponseDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            CompanyId = user.CompanyId
        };
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken);

        if (user is null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }  
}
