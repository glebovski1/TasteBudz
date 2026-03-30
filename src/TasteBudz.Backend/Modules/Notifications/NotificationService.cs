using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Repository-backed notification service used by workflow services and the notification center.
/// </summary>
public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IPersistenceTransactionRunner? transactionRunner = null) : INotificationService
{
    private readonly IPersistenceTransactionRunner transactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public Task CreateAsync(Notification notification, CancellationToken cancellationToken = default) =>
        transactionRunner.ExecuteAsync(() => notificationRepository.SaveAsync(notification, cancellationToken), cancellationToken);

    public Task<IReadOnlyCollection<Notification>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        notificationRepository.ListForUserAsync(userId, cancellationToken);

    public async Task MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default)
    {
        var notification = await notificationRepository.GetAsync(notificationId, cancellationToken);

        if (notification is not null && notification.RecipientUserId == userId)
        {
            await transactionRunner.ExecuteAsync(
                () => notificationRepository.SaveAsync(notification with { ReadAtUtc = readAtUtc }, cancellationToken),
                cancellationToken);
        }
    }
}
