using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Web.Mvc.ViewModels;

/// <summary>
/// Represents a single entry in the chat inbox list.
/// </summary>
public sealed class ChatInboxItemViewModel
{
    public Guid ScopeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ScopeLabel { get; init; } = string.Empty; // "Event" or "Group"
    public string ChatUrl { get; init; } = string.Empty;
}

/// <summary>
/// Page model for the chat inbox — lists all event and group chats the user belongs to.
/// </summary>
public sealed class ChatInboxViewModel
{
    public IReadOnlyList<ChatInboxItemViewModel> Items { get; init; } = [];

    public static ChatInboxViewModel FromDtos(
        IEnumerable<DashboardEventSummaryDto> events,
        IEnumerable<DashboardGroupSummaryDto> groups)
    {
        var items = new List<ChatInboxItemViewModel>();

        foreach (var e in events.Where(CanShowEventChat))
        {
            items.Add(new ChatInboxItemViewModel
            {
                ScopeId = e.EventId,
                Name = string.IsNullOrWhiteSpace(e.Title) ? "Untitled Event" : e.Title,
                ScopeLabel = "Event",
                ChatUrl = $"/Messaging/EventChat?eventId={e.EventId}",
            });
        }

        foreach (var g in groups)
        {
            items.Add(new ChatInboxItemViewModel
            {
                ScopeId = g.GroupId,
                Name = g.Name,
                ScopeLabel = "Group",
                ChatUrl = $"/Messaging/GroupChat?groupId={g.GroupId}",
            });
        }

        return new ChatInboxViewModel { Items = items };
    }

    private static bool CanShowEventChat(DashboardEventSummaryDto e) =>
        e.IsJoined && e.Status is not EventStatus.Cancelled and not EventStatus.Completed;

    public static ChatInboxViewModel Empty => new();
}
