using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Qubix.Core.Authorization;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class AuthorizationTests(
    AuthWebApplicationFactory factory) :
    IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task AuthenticatedEndpoint_WithoutCookie_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            "/_test/authorization/authenticated");
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "authentication_required",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ParticipantCanAccess()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Participant);

        var response = await client.GetAsync(
            "/_test/authorization/authenticated");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task OrganizerEndpoint_ParticipantReceivesForbidden()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Participant);

        var response = await client.PostAsync(
            "/_test/authorization/organizer-only",
            content: null);
        var problem = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OrganizerEndpoint_OrganizerCanAccess()
    {
        using var client = CreateClient();
        await RegisterAsync(client, ApplicationRoles.Organizer);

        var response = await client.PostAsync(
            "/_test/authorization/organizer-only",
            content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task OwnerEndpoint_NonOwnerReceivesForbidden()
    {
        using var client = CreateClient();
        var userId = await RegisterAsync(
            client,
            ApplicationRoles.Organizer);

        var ownerResponse = await client.GetAsync(
            $"/_test/authorization/owned/{userId}");
        var nonOwnerResponse = await client.GetAsync(
            $"/_test/authorization/owned/{Guid.NewGuid()}");
        var problem = await ReadJsonAsync(nonOwnerResponse);

        Assert.Equal(HttpStatusCode.NoContent, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, nonOwnerResponse.StatusCode);
        Assert.Equal(
            "access_denied",
            problem.GetProperty("code").GetString());
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });
    }

    private static async Task<Guid> RegisterAsync(
        HttpClient client,
        string role)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "Password1",
                displayName = "Authorization Test",
                role
            });
        var user = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return user.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ReadJsonAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}
