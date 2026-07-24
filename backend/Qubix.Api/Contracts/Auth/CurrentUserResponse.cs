namespace Qubix.Api.Contracts.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles);
