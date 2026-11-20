using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Data;

// Creates the first SuperAdmin so someone can log in on an empty local database.
// Runs only in Development (see Program.cs). Credentials come from user secrets:
//   SeedSuperAdmin:Email, SeedSuperAdmin:Password (required)
//   SeedSuperAdmin:FirstName, SeedSuperAdmin:LastName (optional)
// It does nothing if a non-deleted SuperAdmin already exists, so restarts are safe.
public static class DevelopmentSeeder
{
    public const string SectionName = "SeedSuperAdmin";

    public static async Task SeedSuperAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentSeeder));

        if (await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin && !u.IsDeleted, cancellationToken))
        {
            return;
        }

        var section = configuration.GetSection(SectionName);
        var email = section["Email"]?.Trim();
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning(
                "No SuperAdmin exists and '{Section}:Email'/'{Section}:Password' are not set in user secrets. Skipping the seed.",
                SectionName, SectionName);
            return;
        }

        var user = new User
        {
            CompanyId = null, // SuperAdmin has no company
            FirstName = section["FirstName"] ?? "Super",
            LastName = section["LastName"] ?? "Admin",
            Email = email,
            Role = UserRole.SuperAdmin
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        // Log the email only, never the password.
        logger.LogInformation("Seeded SuperAdmin {Email}.", email);
    }
}
