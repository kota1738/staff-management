using System.Net;
using System.Net.Http.Json;
using StaffManagement.Api.Contracts;
using StaffManagement.Api.Models;

namespace StaffManagement.Tests;

public sealed class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Health_endpoint_is_anonymous()
    {
        var response = await Client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Staff_endpoints_require_authentication()
    {
        var response = await Client.GetAsync("/api/staff");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(StaffApiFactory.AdminEmail, "not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_an_unknown_account_looks_the_same_as_a_wrong_password()
    {
        var unknown = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("nobody@test.local", "whatever-password"));
        var wrong = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(StaffApiFactory.AdminEmail, "whatever-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(
            await wrong.Content.ReadAsStringAsync(),
            await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Successful_login_exposes_the_users_role()
    {
        await SignedInAsAdminAsync();

        var me = await Client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.NotNull(me);
        Assert.Equal(StaffApiFactory.AdminEmail, me!.Email);
        Assert.Equal(StaffRole.Admin, me.Role);
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        await SignedInAsAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/auth/me")).StatusCode);

        var logout = await Client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await Client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Only_administrators_can_list_accounts()
    {
        await SignedInAsRoleAsync(StaffRole.Manager, "manager@test.local");

        var response = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
