using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qubix.Core.Authorization;

namespace Qubix.Infrastructure.Identity;

public static class IdentityRoleSeeder
{
    public static async Task SeedIdentityRolesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Qubix.IdentityRoleSeeder");

        foreach (var roleName in ApplicationRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>(roleName));

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => $"{error.Code}: {error.Description}"));

                throw new InvalidOperationException(
                    $"Could not create Identity role '{roleName}': {errors}");
            }

            logger.LogInformation("Created Identity role {RoleName}.", roleName);
        }
    }
}
