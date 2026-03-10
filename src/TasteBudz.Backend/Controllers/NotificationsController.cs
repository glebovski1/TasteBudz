// HTTP endpoints for the in-app notifications API.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/notifications")]
/// <summary>
/// Returns the authenticated user's in-app notifications.
/// </summary>
public sealed class NotificationsController(
    NotificationCenterService notificationCenterService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<NotificationDto>> List(CancellationToken cancellationToken) =>
        notificationCenterService.ListAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPatch("{notificationId:guid}")]
    public Task<NotificationDto> Update(Guid notificationId, [FromBody] UpdateNotificationRequest request, CancellationToken cancellationToken) =>
        notificationCenterService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, notificationId, request, cancellationToken);
}
