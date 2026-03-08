// HTTP endpoints for user blocking and unblock operations.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/blocks")]
/// <summary>
/// Manages the authenticated user's block list.
/// </summary>
public sealed class BlocksController(BlockingService blockingService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<BlockedUserDto>> List(CancellationToken cancellationToken) =>
        blockingService.ListAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPost]
    public Task<BlockedUserDto> Create([FromBody] CreateBlockRequest request, CancellationToken cancellationToken) =>
        blockingService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, request, cancellationToken);

    [HttpDelete("{blockedUserId:guid}")]
    public async Task<IActionResult> Remove(Guid blockedUserId, CancellationToken cancellationToken)
    {
        await blockingService.RemoveAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, blockedUserId, cancellationToken);
        return NoContent();
    }
}