using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Auth;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies migrations on SQL Server and creates the schema on SQLite, then
    /// seeds the first administrator if no accounts exist and a seed password is
    /// configured. Without a configured password nothing is seeded, so a
    /// deployment cannot silently create a known account.
    /// </summary>
    public static async Task InitialiseAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StaffDbContext>();

        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new AppUser
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(password),
            Role = StaffRole.Admin,
        });
        await db.SaveChangesAsync();
    }
}
