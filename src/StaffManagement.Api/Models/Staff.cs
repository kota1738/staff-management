using System.ComponentModel.DataAnnotations;

namespace StaffManagement.Api.Models;

/// <summary>A member of staff. Email is unique and enforced by the database.</summary>
public sealed class Staff
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Department { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public decimal? Salary { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}
