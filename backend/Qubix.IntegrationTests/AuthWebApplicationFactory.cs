using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qubix.Infrastructure.Persistence;

namespace Qubix.IntegrationTests;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringVariable =
        "ConnectionStrings__DefaultConnection";
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=qubix_auth_tests;" +
        "Username=qubix;Password=auth-tests-only";

    private readonly string? _originalConnectionString =
        Environment.GetEnvironmentVariable(ConnectionStringVariable);
    private readonly string _databaseName = $"qubix-auth-{Guid.NewGuid()}";

    public AuthWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            ConnectionStringVariable,
            TestConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Identity:SeedRolesOnStartup", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services
                .AddControllers()
                .PartManager.ApplicationParts.Add(
                    new AssemblyPart(typeof(TestAuthorizationController).Assembly));
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
