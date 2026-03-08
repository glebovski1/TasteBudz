// In-memory notification storage used by workflow services and tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Stores notifications in the shared in-memory backing store.
/// </summary>
public sealed class InMemoryNotificationService(InMemoryTasteBudzStore store) : INotificationService
{
    public Task CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Notifications[notification.Id] = notification;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<Notification>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Notifications.Values
                .Where(notification => notification.RecipientUserId == userId)
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Notification>>(items);
        }
    }

    public Task MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (store.Notifications.TryGetValue(notificationId, out var notification) && notification.RecipientUserId == userId)
            {
                store.Notifications[notificationId] = notification with { ReadAtUtc = readAtUtc };
            }

            return Task.CompletedTask;
        }
    }
}