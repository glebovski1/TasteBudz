// Read-side notification-center API workflows.
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Exposes user-scoped notification listing and read-state updates.
/// </summary>
public sealed class NotificationCenterService(
    INotificationService notificationService,
    IClock clock)
{
    public async Task<IReadOnlyCollection<NotificationDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await notificationService.ListForUserAsync(userId, cancellationToken);
        return notifications
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.NotificationType,
                notification.ContextType,
                notification.ContextId,
                notification.Message,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToArray();
    }

    public async Task<NotificationDto> UpdateAsync(Guid userId, Guid notificationId, UpdateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Read != true)
        {
            throw ApiException.BadRequest("Only read=true is supported by the MVP notification API.");
        }

        await notificationService.MarkReadAsync(userId, notificationId, clock.UtcNow, cancellationToken);
        var notification = (await notificationService.ListForUserAsync(userId, cancellationToken))
            .FirstOrDefault(candidate => candidate.Id == notificationId)
            ?? throw ApiException.NotFound("The requested notification could not be found.");

        return new NotificationDto(
            notification.Id,
            notification.NotificationType,
            notification.ContextType,
            notification.ContextId,
            notification.Message,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
    }
}
