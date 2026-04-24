using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Web.Mvc.ViewModels;

public sealed class NotificationsViewModel
{
    public IReadOnlyList<NotificationItem> Unread { get; init; } = [];
    public IReadOnlyList<NotificationItem> Read { get; init; } = [];

    public int UnreadCount => Unread.Count;
    public bool HasAny => Unread.Count > 0 || Read.Count > 0;

    public static NotificationsViewModel Empty => new();

    public static NotificationsViewModel FromDtos(IEnumerable<NotificationDto> dtos)
    {
        var items = dtos
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(NotificationItem.FromDto)
            .ToList();

        return new NotificationsViewModel
        {
            Unread = items.Where(n => !n.IsRead).ToList(),
            Read = items.Where(n => n.IsRead).ToList(),
        };
    }

    /// <summary>
    /// Builds a deep-link URL for a notification based on its context.
    /// Returns null for notifications with no meaningful navigation target.
    /// </summary>
    public static string? BuildLink(string contextType, Guid? contextId)
    {
        if (contextId is null) return null;

        return contextType switch
        {
            "Event" => $"/Event/EventDetails?eventId={contextId}",
            "BudConnection" => "/Profile/View",
            "Group" => $"/Group/Manage?groupId={contextId}",
            "GroupInvite" => null,
            _ => null,
        };
    }
}

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
