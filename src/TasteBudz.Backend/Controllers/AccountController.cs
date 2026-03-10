// HTTP endpoints for account-level actions that do not fit the profile resource.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Auth;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/account")]
/// <summary>
/// Exposes account-management operations for the authenticated user.
/// </summary>
public sealed class AccountController(AuthService authService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost("deletion")]
    public async Task<IActionResult> RequestDeletion(CancellationToken cancellationToken)
    {
        await authService.DeleteAccountAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);
        return NoContent();
    }
}