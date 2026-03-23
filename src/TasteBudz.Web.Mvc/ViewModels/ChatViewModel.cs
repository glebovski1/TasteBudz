using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Web.Mvc.ViewModels;

/// <summary>
/// Passed to the shared Chat view for both event and group scopes.
/// </summary>
public sealed class ChatViewModel
{
    public ChatScopeType ScopeType { get; init; }
    public Guid ScopeId { get; init; }
    public IReadOnlyList<ChatMessageDto> History { get; init; } = [];

    /// <summary>Human-readable title shown at the top of the chat panel.</summary>
    public string Title => ScopeType == ChatScopeType.Event ? "Event Chat" : "Group Chat";

    public static ChatViewModel ForEvent(Guid eventId, IEnumerable<ChatMessageDto> history) =>
        new() { ScopeType = ChatScopeType.Event, ScopeId = eventId, History = history.ToList() };

    public static ChatViewModel ForGroup(Guid groupId, IEnumerable<ChatMessageDto> history) =>
        new() { ScopeType = ChatScopeType.Group, ScopeId = groupId, History = history.ToList() };
}