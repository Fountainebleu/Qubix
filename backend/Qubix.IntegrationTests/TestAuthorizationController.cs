using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qubix.Api.Authorization;
using Qubix.Core.Authorization;

namespace Qubix.IntegrationTests;

[ApiController]
[Route("_test/authorization")]
public sealed class TestAuthorizationController : ControllerBase
{
    [Authorize]
    [HttpGet("authenticated")]
    public IActionResult Authenticated()
    {
        return NoContent();
    }

    [Authorize(Roles = ApplicationRoles.Organizer)]
    [HttpPost("organizer-only")]
    public IActionResult OrganizerOnly()
    {
        return NoContent();
    }

    [Authorize]
    [HttpGet("owned/{ownerId:guid}")]
    public IActionResult Owned(Guid ownerId)
    {
        User.EnsureOwner(ownerId);

        return NoContent();
    }
}
