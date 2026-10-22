using System.ComponentModel.DataAnnotations;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Contracts;

public sealed record StaffWriteRequest(
    [property: Required, MaxLength(80)] string FirstName,
    [property: Required, MaxLength(80)] string LastName,
    [property: Required, EmailAddress, MaxLength(200)] string Email,
    [property: Required, MaxLength(120)] string JobTitle,
    [property: Required, MaxLength(80)] string Department,
    [property: Required] DateOnly StartDate,
    [property: Range(0, 1_000_000)] decimal? Salary);

public sealed record StaffResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string Department,
    DateOnly StartDate,
    decimal? Salary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static StaffResponse From(Staff staff) => new(
        staff.Id,
        staff.FirstName,
        staff.LastName,
        staff.Email,
        staff.JobTitle,
        staff.Department,
        staff.StartDate,
        staff.Salary,
        staff.CreatedAt,
        staff.UpdatedAt);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(1)] string Password);

public sealed record CurrentUserResponse(string Email, StaffRole Role);

public sealed record CreateUserRequest(
    [property: Required, EmailAddress, MaxLength(200)] string Email,
    [property: Required, MinLength(12), MaxLength(200)] string Password,
    [property: Required] StaffRole Role);

public sealed record UserResponse(int Id, string Email, StaffRole Role, DateTimeOffset CreatedAt);
