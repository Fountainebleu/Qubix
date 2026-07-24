using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Qubix.Api.Contracts.Auth;
using Qubix.Core.Authorization;
using Qubix.Core.Exceptions;
using Qubix.Infrastructure.Identity;

namespace Qubix.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TimeProvider timeProvider,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CurrentUserResponse>> Register(
        RegisterRequest request)
    {
        var role = NormalizeRole(request.Role);
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException(
                "An account with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            ThrowIdentityErrors(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            ThrowIdentityErrors(roleResult);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation(
            "Registered user {UserId} with role {Role}.",
            user.Id,
            role);

        var response = await CreateResponseAsync(user);

        return CreatedAtAction(nameof(GetCurrentUser), response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Login(
        LoginRequest request)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            throw new AuthenticationRequiredException(
                "The email address or password is incorrect.");
        }

        var signInResult = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            throw new AuthenticationRequiredException(
                signInResult.IsLockedOut
                    ? "The account is temporarily locked."
                    : "The email address or password is incorrect.");
        }

        logger.LogInformation("User {UserId} signed in.", user.Id);

        return Ok(await CreateResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        logger.LogInformation("User {UserId} signed out.", userId);

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            throw new AuthenticationRequiredException(
                "The authenticated account no longer exists.");
        }

        return Ok(await CreateResponseAsync(user));
    }

    private async Task<CurrentUserResponse> CreateResponseAsync(
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return new CurrentUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.Order(StringComparer.Ordinal).ToArray());
    }

    private static string NormalizeRole(string role)
    {
        if (string.Equals(
                role?.Trim(),
                ApplicationRoles.Participant,
                StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationRoles.Participant;
        }

        if (string.Equals(
                role?.Trim(),
                ApplicationRoles.Organizer,
                StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationRoles.Organizer;
        }

        throw new RequestValidationException(
            "role",
            $"Role must be '{ApplicationRoles.Participant}' or '{ApplicationRoles.Organizer}'.");
    }

    private static void ThrowIdentityErrors(IdentityResult result)
    {
        var errors = result.Errors.ToArray();

        if (errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new ConflictException(
                "An account with this email address already exists.");
        }

        var validationErrors = errors
            .GroupBy(GetErrorField)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.Description)
                    .Distinct()
                    .ToArray());

        throw new RequestValidationException(validationErrors);
    }

    private static string GetErrorField(IdentityError error)
    {
        if (error.Code.StartsWith("Password", StringComparison.Ordinal))
        {
            return "password";
        }

        if (error.Code.Contains("Email", StringComparison.Ordinal) ||
            error.Code.Contains("UserName", StringComparison.Ordinal))
        {
            return "email";
        }

        return "request";
    }
}
