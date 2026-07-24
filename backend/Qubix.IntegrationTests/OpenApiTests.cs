using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Qubix.IntegrationTests;

[Collection(WebApplicationCollection.Name)]
public sealed class OpenApiTests(
    QubixWebApplicationFactory factory) :
    IClassFixture<QubixWebApplicationFactory>
{
    [Fact]
    public async Task DevelopmentEnvironment_ExposesOpenApiAndSwaggerUi()
    {
        using var developmentFactory = factory.WithWebHostBuilder(
            builder => builder.UseEnvironment("Development"));
        using var client = developmentFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerResponse = await client.GetAsync("/swagger/index.html");

        openApiResponse.EnsureSuccessStatusCode();
        swaggerResponse.EnsureSuccessStatusCode();

        Assert.Contains(
            "\"openapi\"",
            await openApiResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.Contains(
            "Qubix API",
            await swaggerResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }
}
