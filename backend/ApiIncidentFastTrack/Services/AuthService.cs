using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Auth;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    // Hash of a random throwaway password. Verifying against it when the email
    // is unknown makes "unknown email" take as long as "wrong password",
    // so response times don't reveal which emails exist.
    private static readonly string DummyHash =
        new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        // Soft-deleted users can't log in (no global filter, so excluded explicitly).
        // The SQL Server default collation is case-insensitive, so the email match is too.
        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            _passwordHasher.VerifyHashedPassword(new User(), DummyHash, request.Password);
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // The hash was made with older hasher settings: upgrade it now that we know the password.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Role = user.Role.ToString(),
            ExpiresAt = expiresAt
        };
    }
}
