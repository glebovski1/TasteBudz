using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Internal persistence boundary for notification storage.
/// </summary>
public interface INotificationRepository
{
    Task SaveAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<Notification?> GetAsync(Guid notificationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Notification>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
