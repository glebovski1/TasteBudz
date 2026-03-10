// HTTP endpoint for listing the authenticated user's Budz.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/budz")]
/// <summary>
/// Returns the authenticated user's current mutual Budz connections.
/// </summary>
public sealed class BudzController(
    DiscoveryService discoveryService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<BudConnectionDto>> List(CancellationToken cancellationToken) =>
        discoveryService.ListMyBudzAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);
}
