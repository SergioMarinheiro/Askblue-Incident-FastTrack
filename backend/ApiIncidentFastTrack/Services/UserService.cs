using System.Linq.Expressions;
using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Users;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Tenant isolation: User has NO tenant query filter (login runs unauthenticated, and SuperAdmin
// authors must stay visible on incidents), so this service filters by company explicitly.
// Every lookup goes through VisibleUsers. A user outside the caller's company is 404.
// Soft delete has no global filter either, so deleted users are excluded explicitly.
//
// Role gates on the controller: list/get/delete ManagerOrAbove, create/update AdminOrAbove.
// The finer rules (who may assign or delete which role, acting on yourself) live here.
public class UserService : IUserService
{
    private const string EmailInUseMessage = "This email is already used by another user.";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyResolver _companyResolver;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext db, ICurrentUser currentUser, ICompanyResolver companyResolver, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _currentUser = currentUser;
        _companyResolver = companyResolver;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return await VisibleUsers()
            .AsNoTracking()
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponseDto> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await VisibleUsers()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(ToDtoExpression)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        EnsureMayAssignRole(request.Role);

        // A SuperAdmin user belongs to no company. Every other role belongs to exactly one,
        // chosen by the shared rules (JWT company; a SuperAdmin creator must send CompanyId).
        int? companyId;
        if (request.Role == UserRole.SuperAdmin)
        {
            if (request.CompanyId is not null)
            {
                throw new BusinessRuleException("A SuperAdmin belongs to no company. Leave CompanyId out.");
            }

            companyId = null;
        }
        else
        {
            companyId = await _companyResolver.ResolveForCreateAsync(request.CompanyId, "a user", cancellationToken);
        }

        var email = request.Email.Trim();
        await EnsureEmailIsFreeAsync(email, exceptUserId: null, cancellationToken);

        var user = new User
        {
            CompanyId = companyId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = request.Role
        };
        // Only the hash is stored; the password itself is never saved, logged or returned.
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await SaveCatchingDuplicateEmailAsync(cancellationToken);

        return ToDto(user);
    }

    // Company is not editable (the DTO has no CompanyId). Password is not changed here.
    public async Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await VisibleUsers()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");

        if (request.Role != user.Role)
        {
            EnsureMayAssignRole(request.Role);

            // Decided 2026-11-21: never needed. A SuperAdmin has no company and every other
            // role needs one, so the change would also leave the company wrong.
            if (user.Role == UserRole.SuperAdmin || request.Role == UserRole.SuperAdmin)
            {
                throw new BusinessRuleException("The SuperAdmin role can't be given or taken away. Create a new user instead.");
            }

            // Decided 2026-11-21: e.g. the only Admin of a company demoting themselves.
            if (user.Id == _currentUser.UserId)
            {
                throw new BusinessRuleException("You can't change your own role.");
            }
        }

        var email = request.Email.Trim();
        await EnsureEmailIsFreeAsync(email, exceptUserId: user.Id, cancellationToken);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.Role = request.Role;

        await SaveCatchingDuplicateEmailAsync(cancellationToken);

        // Note: a changed role only reaches the user's token at their next login (see open questions).
        return ToDto(user);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await VisibleUsers()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");

        // Decided 2026-11-21.
        if (user.Id == _currentUser.UserId)
        {
            throw new BusinessRuleException("You can't delete yourself.");
        }

        // Admin: anyone in their company. SuperAdmin: anyone. Manager: only Staff.
        if (_currentUser.Role == UserRole.Manager && user.Role != UserRole.Staff)
        {
            throw new ForbiddenException("A Manager can only delete Staff users.");
        }

        // AppDbContext.SaveChangesAsync turns this into a soft delete (IsDeleted, DeletedAt).
        // The user can no longer log in, and their email can be reused.
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Active users the caller may see: their own company, or every company for a SuperAdmin.
    // Tracked, so update and delete can use it; reads add AsNoTracking.
    private IQueryable<User> VisibleUsers()
    {
        var query = _db.Users.Where(u => !u.IsDeleted);

        if (!_currentUser.IsSuperAdmin)
        {
            // Fails closed: if the caller somehow has no company claim, this matches no user.
            // "u.CompanyId != null" keeps SuperAdmins (CompanyId null) out of a company's list.
            var companyId = _currentUser.CompanyId;
            query = query.Where(u => u.CompanyId != null && u.CompanyId == companyId);
        }

        return query;
    }

    // Only a SuperAdmin may give out the SuperAdmin role. 403: it's about the caller's rights.
    private void EnsureMayAssignRole(UserRole role)
    {
        if (role == UserRole.SuperAdmin && !_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only a SuperAdmin can assign the SuperAdmin role.");
        }
    }

    // Unique among active users, across all companies (login is by email).
    // The SQL Server default collation is case-insensitive, so this match is too.
    private async Task EnsureEmailIsFreeAsync(string email, int? exceptUserId, CancellationToken cancellationToken)
    {
        var inUse = await _db.Users
            .AnyAsync(u => u.Email == email && !u.IsDeleted && u.Id != exceptUserId, cancellationToken);

        if (inUse)
        {
            throw new ConflictException(EmailInUseMessage);
        }
    }

    // The unique index (filtered on IsDeleted = 0) is the real guarantee: if another request took
    // the email between the check and the save, the database refuses it. Same 409, not a 500.
    private async Task SaveCatchingDuplicateEmailAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(EmailInUseMessage);
        }
    }

    private static readonly Expression<Func<User, UserResponseDto>> ToDtoExpression = u => new UserResponseDto
    {
        UserId = u.Id,
        CompanyId = u.CompanyId,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        Role = u.Role
    };

    private static UserResponseDto ToDto(User user) => new()
    {
        UserId = user.Id,
        CompanyId = user.CompanyId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role
    };
}
