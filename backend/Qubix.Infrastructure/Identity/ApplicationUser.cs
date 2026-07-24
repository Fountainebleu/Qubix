using Microsoft.AspNetCore.Identity;

namespace Qubix.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public const int MaxDisplayNameLength = 50;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
