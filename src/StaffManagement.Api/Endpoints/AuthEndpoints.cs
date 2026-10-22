using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Auth;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Data;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("auth");

        group.MapPost("/login", LoginAsync).WithName("Login");
        // Two parameters keep this off the RequestDelegate overload, which would
        // discard the returned IResult and answer 200 instead of 204.
        group.MapPost("/logout", async (HttpContext http, ClaimsPrincipal principal) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization().WithName("Logout");

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? nameof(StaffRole.Viewer);
            return Results.Ok(new CurrentUserResponse(email, Enum.Parse<StaffRole>(role)));
        }).RequireAuthorization().WithName("CurrentUser");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        StaffDbContext db,
        IPasswordHasher hasher,
        HttpContext http,
        CancellationToken ct)
    {
        if (Validation.Validate(request) is { Count: > 0 } errors)
        {
            return Results.ValidationProblem(errors);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same response for an unknown account and a wrong password, so the
        // endpoint cannot be used to enumerate accounts.
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });

        return Results.NoContent();
    }

}
