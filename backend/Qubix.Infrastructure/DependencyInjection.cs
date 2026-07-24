using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qubix.Infrastructure.Persistence;

namespace Qubix.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DefaultConnectionName}' is not configured. " +
                $"Set the 'ConnectionStrings__{DefaultConnectionName}' environment variable.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
