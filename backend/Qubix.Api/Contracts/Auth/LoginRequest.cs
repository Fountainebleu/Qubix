using System.ComponentModel.DataAnnotations;

namespace Qubix.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}
