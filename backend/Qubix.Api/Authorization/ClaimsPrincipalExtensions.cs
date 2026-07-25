using System.Security.Claims;
using Qubix.Core.Exceptions;

namespace Qubix.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId) ||
            parsedUserId == Guid.Empty)
        {
            throw new AuthenticationRequiredException(
                "The authenticated user identifier is missing or invalid.");
        }

        return parsedUserId;
    }

    public static void EnsureOwner(
        this ClaimsPrincipal user,
        Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner identifier cannot be empty.",
                nameof(ownerId));
        }

        if (user.GetRequiredUserId() != ownerId)
        {
            throw new AccessDeniedException(
                "Only the resource owner can perform this action.");
        }
    }
}
