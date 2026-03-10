// Request and response contracts for the in-app notifications API.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Notifications;

/// <summary>
/// Notification payload returned by the notification-center API.
/// </summary>
public sealed record NotificationDto(
    Guid NotificationId,
    NotificationType NotificationType,
    string ContextType,
    Guid? ContextId,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

/// <summary>
/// Request body for notification read-state updates.
/// </summary>
public sealed class UpdateNotificationRequest
{
    [Required]
    public bool? Read { get; init; }
}
