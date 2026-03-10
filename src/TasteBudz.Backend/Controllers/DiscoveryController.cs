// HTTP endpoints for people discovery and swipe workflows.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Discovery;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/discovery")]
/// <summary>
/// Exposes people search, swipe-candidate, and swipe-decision APIs.
/// </summary>
public sealed class DiscoveryController(
    DiscoveryService discoveryService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("people")]
    public Task<ListResponse<DiscoveryProfilePreviewDto>> SearchPeople([FromQuery] SearchPeopleQuery query, CancellationToken cancellationToken) =>
        discoveryService.SearchAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, query, cancellationToken);

    [HttpGet("swipe-candidates")]
    public Task<ListResponse<DiscoveryProfilePreviewDto>> GetSwipeCandidates([FromQuery] SwipeCandidatesQuery query, CancellationToken cancellationToken) =>
        discoveryService.GetSwipeCandidatesAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, query, cancellationToken);

    [HttpPost("swipes")]
    public Task<SwipeDecisionResultDto> RecordSwipe([FromBody] RecordSwipeDecisionRequest request, CancellationToken cancellationToken) =>
        discoveryService.RecordSwipeAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
