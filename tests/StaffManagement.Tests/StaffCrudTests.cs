using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Data;
using StaffManagement.Api.Models;

namespace StaffManagement.Tests;

public sealed class StaffCrudTests : ApiTestBase
{
    [Fact]
    public async Task Viewer_cannot_create_staff()
    {
        await SignedInAsRoleAsync(StaffRole.Viewer, "viewer@test.local");

        var response = await Client.PostAsJsonAsync(
            "/api/staff",
            new StaffWriteRequest("Ada", "Lovelace", "ada@test.local", "Engineer", "R&D", new DateOnly(2024, 1, 1), 40000m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, CountStaff());
    }

    [Fact]
    public async Task Manager_can_create_staff_and_the_row_is_persisted()
    {
        await SignedInAsRoleAsync(StaffRole.Manager, "manager@test.local");

        var staff = await CreateStaffAsync("ada@test.local");

        Assert.True(staff.Id > 0);
        Assert.Equal("ada@test.local", staff.Email);
        Assert.Equal(1, CountStaff());
    }

    [Fact]
    public async Task Create_rejects_an_invalid_payload_with_field_errors()
    {
        await SignedInAsAdminAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/staff",
            new StaffWriteRequest("", "Lovelace", "not-an-email", "Engineer", "", new DateOnly(2024, 1, 1), -5m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("email", problem!.Errors.Keys);
        Assert.Contains("firstName", problem.Errors.Keys);
        Assert.Contains("salary", problem.Errors.Keys);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_with_409()
    {
        await SignedInAsAdminAsync();
        await CreateStaffAsync("duplicate@test.local");

        var response = await Client.PostAsJsonAsync(
            "/api/staff",
            new StaffWriteRequest("Grace", "Hopper", "DUPLICATE@test.local", "Engineer", "R&D", new DateOnly(2024, 1, 1), 40000m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, CountStaff());
    }

    [Fact]
    public async Task Update_changes_fields_and_rejects_unknown_ids()
    {
        await SignedInAsAdminAsync();
        var staff = await CreateStaffAsync("before@test.local", "Operations");

        var updated = await Client.PutAsJsonAsync(
            $"/api/staff/{staff.Id}",
            new StaffWriteRequest("Ada", "Byron", "after@test.local", "Principal Engineer", "Research", new DateOnly(2023, 9, 4), 62000m));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = (await updated.Content.ReadFromJsonAsync<StaffResponse>())!;
        Assert.Equal("after@test.local", body.Email);
        Assert.Equal("Research", body.Department);
        Assert.Equal(62000m, body.Salary);

        var missing = await Client.PutAsJsonAsync(
            "/api/staff/9999",
            new StaffWriteRequest("No", "Body", "none@test.local", "Engineer", "R&D", new DateOnly(2024, 1, 1), null));

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Only_administrators_can_delete()
    {
        await SignedInAsRoleAsync(StaffRole.Manager, "manager@test.local");
        var staff = await CreateStaffAsync("delete-me@test.local");

        var asManager = await Client.DeleteAsync($"/api/staff/{staff.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, asManager.StatusCode);

        await SignedInAsAdminAsync();
        var asAdmin = await Client.DeleteAsync($"/api/staff/{staff.Id}");
        Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
        Assert.Equal(0, CountStaff());
    }

    [Fact]
    public async Task List_filters_by_department_and_pages()
    {
        await SignedInAsAdminAsync();
        await CreateStaffAsync("a@test.local", "Operations");
        await CreateStaffAsync("b@test.local", "Operations");
        await CreateStaffAsync("c@test.local", "Research");

        var research = await Client.GetFromJsonAsync<PagedResponse<StaffResponse>>("/api/staff?department=Research");
        Assert.NotNull(research);
        Assert.Equal(1, research!.Total);

        var page = await Client.GetFromJsonAsync<PagedResponse<StaffResponse>>("/api/staff?page=1&pageSize=2");
        Assert.NotNull(page);
        Assert.Equal(3, page!.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    public async Task Search_matches_name_email_and_job_title()
    {
        await SignedInAsAdminAsync();
        await CreateStaffAsync("searchable@test.local", "Operations");

        var byEmail = await Client.GetFromJsonAsync<PagedResponse<StaffResponse>>("/api/staff?q=searchable");
        var byName = await Client.GetFromJsonAsync<PagedResponse<StaffResponse>>("/api/staff?q=Lovelace");
        var noMatch = await Client.GetFromJsonAsync<PagedResponse<StaffResponse>>("/api/staff?q=nothing-matches");

        Assert.Equal(1, byEmail!.Total);
        Assert.Equal(1, byName!.Total);
        Assert.Equal(0, noMatch!.Total);
    }

    [Fact]
    public async Task Unique_email_is_enforced_by_the_database_not_only_the_api()
    {
        await SignedInAsAdminAsync();
        await CreateStaffAsync("indexed@test.local");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StaffDbContext>();
        db.Staff.Add(new Staff
        {
            FirstName = "Direct",
            LastName = "Insert",
            Email = "indexed@test.local",
            JobTitle = "Engineer",
            Department = "R&D",
            StartDate = new DateOnly(2024, 1, 1),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
