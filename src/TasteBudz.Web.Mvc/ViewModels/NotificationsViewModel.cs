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
