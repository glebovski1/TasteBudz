using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Web.Mvc.ViewModels;


public sealed class NotificationItem
{
    public Guid Id { get; init; }
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string ContextType { get; init; } = string.Empty;
    public Guid? ContextId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public bool IsRead { get; init; }
    public string? Link { get; init; }
    public string Icon { get; init; } = string.Empty;
    public bool CanRespondToGroupInvite =>
        !IsRead &&
        Type == NotificationType.GroupInviteReceived &&
        string.Equals(ContextType, "GroupInvite", StringComparison.OrdinalIgnoreCase) &&
        ContextId.HasValue;

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTimeOffset.UtcNow - CreatedAtUtc;
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}d ago";
            return CreatedAtUtc.LocalDateTime.ToString("MMM d");
        }
    }

    public static NotificationItem FromDto(NotificationDto dto) => new()
    {
        Id = dto.NotificationId,
        Message = dto.Message,
        Type = dto.NotificationType,
        ContextType = dto.ContextType,
        ContextId = dto.ContextId,
        CreatedAtUtc = dto.CreatedAtUtc,
        IsRead = dto.ReadAtUtc is not null,
        Link = NotificationsViewModel.BuildLink(dto.ContextType, dto.ContextId),
        Icon = dto.NotificationType switch
        {
            NotificationType.BudMatched => "🍔",
            NotificationType.EventInviteReceived => "📅",
            NotificationType.EventJoined => "🎉",
            NotificationType.EventLeft => "👋",
            NotificationType.EventConfirmed => "✅",
            NotificationType.EventCancelled => "❌",
            NotificationType.EventUpdated => "📝",
            NotificationType.GroupInviteReceived => "👥",
            _ => "🔔",
        },
    };
}
