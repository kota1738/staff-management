using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Api.Models;

public enum StaffRole
{
    Viewer = 0,
    Manager = 1,
    Admin = 2,
}

/// <summary>An account that can sign in. Passwords are stored only as a hash.</summary>
public sealed class AppUser
{
    public int Id { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string PasswordHash { get; set; } = string.Empty;

    public StaffRole Role { get; set; } = StaffRole.Viewer;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
