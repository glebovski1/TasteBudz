// HTTP endpoints for registration, login, token refresh, and logout.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Controllers;

[ApiController]
[Route("api/v1/auth")]
/// <summary>
/// Handles session lifecycle endpoints.
/// </summary>
public sealed class AuthController(AuthService authService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<SessionDto>> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.RegisterAsync(request, cancellationToken);
        return Ok(session);
    }

    [HttpPost("login")]
    public async Task<ActionResult<SessionDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, cancellationToken);
        return Ok(session);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<SessionDto>> Refresh([FromBody] RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.RefreshAsync(request, cancellationToken);
        return Ok(session);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var currentUser = currentUserAccessor.GetRequiredCurrentUser();
        var authorizationHeader = Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        // Logout targets the exact presented access token so only that session is revoked here.
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw ApiException.Unauthorized();
        }

        await authService.LogoutAsync(currentUser.UserId, authorizationHeader[bearerPrefix.Length..].Trim(), cancellationToken);
        return NoContent();
    }
}