using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Auth;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Data;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization("AdminOnly").WithTags("users");

        group.MapGet("/", async (StaffDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users.AsNoTracking()
                .OrderBy(u => u.Email)
                .Select(u => new UserResponse(u.Id, u.Email, u.Role, u.CreatedAt))
                .ToListAsync(ct);
            return Results.Ok(users);
        }).WithName("ListUsers");

        group.MapPost("/", async (
            CreateUserRequest request,
            StaffDbContext db,
            IPasswordHasher hasher,
            CancellationToken ct) =>
        {
            if (Validation.Validate(request) is { Count: > 0 } errors)
            {
                return Results.ValidationProblem(errors);
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct))
            {
                return Results.Problem(
                    title: "Email already in use",
                    detail: $"An account with the email '{email}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var user = new AppUser
            {
                Email = email,
                PasswordHash = hasher.Hash(request.Password),
                Role = request.Role,
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/users/{user.Id}",
                new UserResponse(user.Id, user.Email, user.Role, user.CreatedAt));
        }).WithName("CreateUser");
    }
}
