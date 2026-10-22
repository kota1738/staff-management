using Microsoft.EntityFrameworkCore;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Data;
using StaffManagement.Api.Models;

namespace StaffManagement.Api.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/staff").RequireAuthorization().WithTags("staff");

        group.MapGet("/", ListStaffAsync)
            .WithName("ListStaff")
            .WithSummary("List staff with search, filter and paging.");

        group.MapGet("/{id:int}", GetStaffAsync)
            .WithName("GetStaff");

        group.MapPost("/", CreateStaffAsync)
            .RequireAuthorization("CanWrite")
            .WithName("CreateStaff");

        group.MapPut("/{id:int}", UpdateStaffAsync)
            .RequireAuthorization("CanWrite")
            .WithName("UpdateStaff");

        group.MapDelete("/{id:int}", DeleteStaffAsync)
            .RequireAuthorization("AdminOnly")
            .WithName("DeleteStaff");
    }

    private static async Task<IResult> ListStaffAsync(
        StaffDbContext db,
        string? q,
        string? department,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Staff.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.FirstName, $"%{term}%") ||
                EF.Functions.Like(s.LastName, $"%{term}%") ||
                EF.Functions.Like(s.Email, $"%{term}%") ||
                EF.Functions.Like(s.JobTitle, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var wanted = department.Trim();
            query = query.Where(s => s.Department == wanted);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var items = rows.Select(StaffResponse.From).ToList();

        return Results.Ok(new PagedResponse<StaffResponse>(items, total, page, pageSize));
    }

    private static async Task<IResult> GetStaffAsync(int id, StaffDbContext db, CancellationToken ct)
    {
        var staff = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return staff is null ? Results.NotFound() : Results.Ok(StaffResponse.From(staff));
    }

    private static async Task<IResult> CreateStaffAsync(
        StaffWriteRequest request,
        StaffDbContext db,
        CancellationToken ct)
    {
        if (Validation.Validate(request) is { Count: > 0 } errors)
        {
            return Results.ValidationProblem(errors);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Staff.AnyAsync(s => s.Email == email, ct))
        {
            return EmailInUse(email);
        }

        var staff = new Staff
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            JobTitle = request.JobTitle.Trim(),
            Department = request.Department.Trim(),
            StartDate = request.StartDate,
            Salary = request.Salary,
        };

        db.Staff.Add(staff);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two requests raced past the check above; the unique index caught it.
            if (await db.Staff.AnyAsync(s => s.Email == email, ct))
            {
                return EmailInUse(email);
            }

            throw;
        }

        var location = $"/api/staff/{staff.Id}";
        return Results.Created(location, StaffResponse.From(staff));
    }

    private static async Task<IResult> UpdateStaffAsync(
        int id,
        StaffWriteRequest request,
        StaffDbContext db,
        CancellationToken ct)
    {
        if (Validation.Validate(request) is { Count: > 0 } errors)
        {
            return Results.ValidationProblem(errors);
        }

        var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (staff is null)
        {
            return Results.NotFound();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Staff.AnyAsync(s => s.Email == email && s.Id != id, ct))
        {
            return EmailInUse(email);
        }

        staff.FirstName = request.FirstName.Trim();
        staff.LastName = request.LastName.Trim();
        staff.Email = email;
        staff.JobTitle = request.JobTitle.Trim();
        staff.Department = request.Department.Trim();
        staff.StartDate = request.StartDate;
        staff.Salary = request.Salary;
        staff.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return EmailInUse(email);
        }

        return Results.Ok(StaffResponse.From(staff));
    }

    private static async Task<IResult> DeleteStaffAsync(int id, StaffDbContext db, CancellationToken ct)
    {
        var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (staff is null)
        {
            return Results.NotFound();
        }

        db.Staff.Remove(staff);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IResult EmailInUse(string email) => Results.Problem(
        title: "Email already in use",
        detail: $"A member of staff with the email '{email}' already exists.",
        statusCode: StatusCodes.Status409Conflict);
}
