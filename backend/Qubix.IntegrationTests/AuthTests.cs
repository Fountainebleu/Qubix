using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Qubix.Core.Authorization;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class AuthTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    [Fact]
    public async Task Startup_CreatesParticipantAndOrganizerRoles()
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Participant));
        Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Organizer));
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organizer")]
    public async Task Register_SignsInUserWithSelectedRole(string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var response = await RegisterAsync(_client, email, role);
        var registeredUser = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(email, registeredUser.GetProperty("email").GetString());
        Assert.Equal("Test User", registeredUser.GetProperty("displayName").GetString());
        Assert.Equal(
            role,
            registeredUser.GetProperty("roles")[0].GetString());

        var meResponse = await _client.GetAsync("/api/auth/me");
        var currentUser = await ReadJsonAsync(meResponse);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Equal(
            registeredUser.GetProperty("id").GetGuid(),
            currentUser.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflictProblemDetails()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var firstResponse = await RegisterAsync(
            _client,
            email,
            ApplicationRoles.Participant);
        var duplicateResponse = await RegisterAsync(
            _client,
            email.ToUpperInvariant(),
            ApplicationRoles.Participant);
        var problem = await ReadJsonAsync(duplicateResponse);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal("conflict", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task LoginLogout_ControlsAccessToCurrentUser()
    {
        var email = $"session-{Guid.NewGuid():N}@example.com";
        var registrationResponse = await RegisterAsync(
            _client,
            email,
            ApplicationRoles.Participant);
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        var afterLogoutResponse = await _client.GetAsync("/api/auth/me");
        var unauthorizedProblem = await ReadJsonAsync(afterLogoutResponse);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResponse.StatusCode);
        Assert.Equal(
            "authentication_required",
            unauthorizedProblem.GetProperty("code").GetString());

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = "Password1",
                rememberMe = false
            });
        var meResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedProblemDetails()
    {
        var email = $"wrong-password-{Guid.NewGuid():N}@example.com";
        var registrationResponse = await RegisterAsync(
            _client,
            email,
            ApplicationRoles.Participant);
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        await _client.PostAsync("/api/auth/logout", null);
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                password = "WrongPassword1",
                rememberMe = false
            });
        var problem = await ReadJsonAsync(loginResponse);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.Equal(
            "authentication_required",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsValidationProblemDetails()
    {
        var response = await RegisterAsync(
            _client,
            $"invalid-role-{Guid.NewGuid():N}@example.com",
            "Administrator");
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("role", out _));
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string role)
    {
        return client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                password = "Password1",
                displayName = "Test User",
                role
            });
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
