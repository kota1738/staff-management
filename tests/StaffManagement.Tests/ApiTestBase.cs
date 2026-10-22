using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Data;
using StaffManagement.Api.Models;

namespace StaffManagement.Tests;

/// <summary>
/// One factory (and therefore one database) per test, so tests never depend on
/// each other's rows or on execution order.
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected StaffApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Factory = new StaffApiFactory();
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    protected async Task<HttpClient> SignedInAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return Client;
    }

    protected Task<HttpClient> SignedInAsAdminAsync() => SignedInAsync(StaffApiFactory.AdminEmail, StaffApiFactory.AdminPassword);

    /// <summary>Creates an account through the admin-only endpoint and signs in as it.</summary>
    protected async Task<HttpClient> SignedInAsRoleAsync(StaffRole role, string email)
    {
        const string password = "another-password-123";
        await SignedInAsAdminAsync();

        var created = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(email, password, role));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        return await SignedInAsync(email, password);
    }

    protected async Task<StaffResponse> CreateStaffAsync(string email, string department = "Operations")
    {
        var response = await Client.PostAsJsonAsync(
            "/api/staff",
            new StaffWriteRequest("Ada", "Lovelace", email, "Engineer", department, new DateOnly(2024, 3, 1), 45000m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StaffResponse>())!;
    }

    protected int CountStaff()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StaffDbContext>();
        return db.Staff.Count();
    }
}
