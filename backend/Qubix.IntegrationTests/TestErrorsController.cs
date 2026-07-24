using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Qubix.Core.Common;
using Qubix.Core.Exceptions;

namespace Qubix.IntegrationTests;

[ApiController]
[Route("_test/errors")]
public sealed class TestErrorsController : ControllerBase
{
    [HttpGet("validation")]
    public IActionResult ValidationError()
    {
        throw new RequestValidationException("name", "Name is required.");
    }

    [HttpGet("authentication")]
    public IActionResult AuthenticationError()
    {
        throw new AuthenticationRequiredException("Sign in to access this resource.");
    }

    [HttpGet("access")]
    public IActionResult AccessError()
    {
        throw new AccessDeniedException("You do not have access to this resource.");
    }

    [HttpGet("not-found")]
    public IActionResult NotFoundError()
    {
        throw new NotFoundException("Quiz was not found.");
    }

    [HttpGet("conflict")]
    public IActionResult ConflictError()
    {
        throw new ConflictException("The room code is already in use.");
    }

    [HttpGet("domain-conflict")]
    public IActionResult DomainConflictError()
    {
        throw new DomainException("The quiz session is not waiting.");
    }

    [HttpGet("unhandled")]
    public IActionResult UnhandledError()
    {
        throw new InvalidOperationException("This detail must not be exposed.");
    }

    [HttpPost("model-validation")]
    public IActionResult ModelValidation([FromBody] TestValidationRequest request)
    {
        return Ok(request);
    }

    [HttpGet("status/{statusCode:int}")]
    public IActionResult EmptyStatusCode(int statusCode)
    {
        return StatusCode(statusCode);
    }
}

public sealed record TestValidationRequest(
    [Required] string? Name);
