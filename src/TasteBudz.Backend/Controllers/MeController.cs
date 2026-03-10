// HTTP endpoints that expose dashboard-style views for the authenticated user.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me")]
/// <summary>
/// Returns user-centric summary views composed from multiple modules.
/// </summary>
public sealed class MeController(DashboardService dashboardService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardDto> GetDashboard(CancellationToken cancellationToken) =>
        dashboardService.GetDashboardAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpGet("events")]
    public Task<IReadOnlyCollection<DashboardEventSummaryDto>> ListEvents(CancellationToken cancellationToken) =>
        dashboardService.ListMyEventsAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpGet("groups")]
    public Task<IReadOnlyCollection<DashboardGroupSummaryDto>> ListGroups(CancellationToken cancellationToken) =>
        dashboardService.ListMyGroupsAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpGet("event-invites")]
    public Task<IReadOnlyCollection<EventInviteDto>> ListEventInvites(CancellationToken cancellationToken) =>
        dashboardService.ListMyEventInvitesAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);
}