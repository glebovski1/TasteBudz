using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Notifications;

public sealed record NotificationDto(
    Guid NotificationId,
    NotificationType NotificationType,
    string ContextType,
    Guid? ContextId,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);
