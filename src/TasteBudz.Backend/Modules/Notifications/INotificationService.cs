// Application service boundary for storing and reading user notifications.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Handles notification persistence for workflow services.
/// </summary>
public interface INotificationService
{
    Task CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Notification>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default);
}