using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// SQLite-backed notification repository.
/// </summary>
public sealed class SqliteNotificationRepository(TasteBudzDbContext dbContext) : INotificationRepository
{
    public async Task SaveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notifications.FirstOrDefaultAsync(item => item.Id == notification.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.Notifications.Add(ToEntity(notification));
        }
        else
        {
            entity.RecipientUserId = notification.RecipientUserId;
            entity.NotificationType = notification.NotificationType;
            entity.ContextType = notification.ContextType;
            entity.ContextId = notification.ContextId;
            entity.Message = notification.Message;
            entity.CreatedAtUtc = notification.CreatedAtUtc;
            entity.ReadAtUtc = notification.ReadAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Notification?> GetAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(item => item.Id == notificationId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyCollection<Notification>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.RecipientUserId == userId)
            .ToListAsync(cancellationToken))
        .Select(Map)
        .OrderByDescending(item => item.CreatedAtUtc)
        .ToArray();

    private static Notification Map(NotificationEntity entity) =>
        new(
            entity.Id,
            entity.RecipientUserId,
            entity.NotificationType,
            entity.ContextType,
            entity.ContextId,
            entity.Message,
            entity.CreatedAtUtc,
            entity.ReadAtUtc);

    private static NotificationEntity ToEntity(Notification item) =>
        new()
        {
            Id = item.Id,
            RecipientUserId = item.RecipientUserId,
            NotificationType = item.NotificationType,
            ContextType = item.ContextType,
            ContextId = item.ContextId,
            Message = item.Message,
            CreatedAtUtc = item.CreatedAtUtc,
            ReadAtUtc = item.ReadAtUtc,
        };
}
