using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class ErrorHandlingTests(
    QubixWebApplicationFactory factory) :
    IClassFixture<QubixWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    [Fact]
    public async Task ValidationException_ReturnsValidationProblemDetails()
    {
        var (response, problem) = await GetProblemAsync("_test/errors/validation");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response, problem, 400, "validation_error");
        Assert.True(problem.GetProperty("errors").TryGetProperty("name", out var errors));
        Assert.Equal("Name is required.", errors[0].GetString());
    }

    [Fact]
    public async Task InvalidModel_ReturnsSameValidationFormat()
    {
        var response = await _client.PostAsJsonAsync(
            "_test/errors/model-validation",
            new { });
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response, problem, 400, "validation_error");
        Assert.Equal(JsonValueKind.Object, problem.GetProperty("errors").ValueKind);
        Assert.NotEmpty(problem.GetProperty("errors").EnumerateObject());
    }

    [Theory]
    [InlineData("authentication", 401, "authentication_required")]
    [InlineData("access", 403, "access_denied")]
    [InlineData("not-found", 404, "resource_not_found")]
    [InlineData("conflict", 409, "conflict")]
    [InlineData("domain-conflict", 409, "conflict")]
    public async Task KnownException_ReturnsMappedProblemDetails(
        string endpoint,
        int expectedStatus,
        string expectedCode)
    {
        var (response, problem) = await GetProblemAsync($"_test/errors/{endpoint}");

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        AssertProblemDetails(
            response,
            problem,
            expectedStatus,
            expectedCode);
    }

    [Fact]
    public async Task UnhandledException_DoesNotExposeInternalMessage()
    {
        var (response, problem) = await GetProblemAsync("_test/errors/unhandled");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertProblemDetails(response, problem, 500, "internal_error");
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain(
            "must not be exposed",
            problem.GetRawText(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(401, "authentication_required")]
    [InlineData(403, "access_denied")]
    [InlineData(404, "resource_not_found")]
    public async Task EmptyErrorStatus_ReturnsProblemDetails(
        int statusCode,
        string expectedCode)
    {
        var (response, problem) = await GetProblemAsync(
            $"_test/errors/status/{statusCode}");

        Assert.Equal(statusCode, (int)response.StatusCode);
        AssertProblemDetails(response, problem, statusCode, expectedCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFoundProblemDetails()
    {
        var (response, problem) = await GetProblemAsync("route-that-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response, problem, 404, "resource_not_found");
    }

    private async Task<(HttpResponseMessage Response, JsonElement Problem)> GetProblemAsync(
        string requestUri)
    {
        var response = await _client.GetAsync(requestUri);
        var problem = await ReadProblemAsync(response);

        return (response, problem);
    }

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    private static void AssertProblemDetails(
        HttpResponseMessage response,
        JsonElement problem,
        int expectedStatus,
        string expectedCode)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("instance").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }
}
