using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Auth;
using StaffManagement.Api.Data;
using StaffManagement.Api.Endpoints;
using StaffManagement.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=staff.db";

builder.Services.AddDbContext<StaffDbContext>(options =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "staff.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        // An API answers with status codes; it must not redirect to a login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanWrite", policy => policy.RequireRole(
        nameof(StaffRole.Admin), nameof(StaffRole.Manager)));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(StaffRole.Admin)));
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
    .WithTags("health")
    .AllowAnonymous();

app.MapStaffEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

await DatabaseInitializer.InitialiseAsync(app.Services, app.Configuration);

app.Run();

/// <summary>Exposed so the integration tests can boot the real pipeline.</summary>
public partial class Program { }
