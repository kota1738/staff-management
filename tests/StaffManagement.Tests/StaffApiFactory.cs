using Microsoft.AspNetCore.Mvc.Testing;

namespace StaffManagement.Tests;

/// <summary>
/// Boots the real pipeline (DI, auth, EF Core, endpoints) against a throwaway
/// SQLite file. Configuration is supplied through environment variables because
/// the API reads its provider and connection string while building the host.
/// </summary>
public sealed class StaffApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "admin-password-123";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"staff-tests-{Guid.NewGuid():N}.db");

    public StaffApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_databasePath}");
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // A pooled connection may still hold the file on Windows; the
                // temp directory is cleaned by the OS.
            }
        }
    }
}
