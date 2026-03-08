// Notification records produced by backend workflows.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// User notification payload stored by the backend.
/// </summary>
public sealed record Notification(
    Guid Id,
    Guid RecipientUserId,
    NotificationType NotificationType,
    string ContextType,
    Guid? ContextId,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);