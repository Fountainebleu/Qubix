using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Qubix.IntegrationTests;

public sealed class QubixWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringVariable =
        "ConnectionStrings__DefaultConnection";
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=qubix_api_tests;" +
        "Username=qubix;Password=api-tests-only";

    private readonly string? _originalConnectionString =
        Environment.GetEnvironmentVariable(ConnectionStringVariable);

    public QubixWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringVariable,
            TestConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services
                .AddControllers()
                .PartManager.ApplicationParts.Add(
                    new AssemblyPart(typeof(TestErrorsController).Assembly));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable(
            ConnectionStringVariable,
            _originalConnectionString);
    }
}
