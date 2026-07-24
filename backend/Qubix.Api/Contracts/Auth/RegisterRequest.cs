using System.ComponentModel.DataAnnotations;
using Qubix.Core.Authorization;
using Qubix.Infrastructure.Identity;

namespace Qubix.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(ApplicationUser.MaxDisplayNameLength)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = ApplicationRoles.Participant;
}
