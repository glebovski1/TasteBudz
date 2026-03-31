// HTTP endpoint for listing and removing the authenticated user's Budz.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/budz")]
/// <summary>
/// Returns and manages the authenticated user's mutual Budz connections.
/// </summary>
public sealed class BudzController(
    DiscoveryService discoveryService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<BudConnectionDto>> List(CancellationToken cancellationToken) =>
        discoveryService.ListMyBudzAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpDelete("{otherUserId:guid}")]
    public async Task<IActionResult> Remove(Guid otherUserId, CancellationToken cancellationToken)
    {
        await discoveryService.RemoveBudAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, otherUserId, cancellationToken);
        return NoContent();
    }
}