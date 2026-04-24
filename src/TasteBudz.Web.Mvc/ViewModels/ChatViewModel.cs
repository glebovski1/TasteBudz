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
    public string HubUrl { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string? ContextName { get; init; }
    public string? ContextSummary { get; init; }
    public string? BackLinkUrl { get; init; }
    public string? BackLinkText { get; init; }

    /// <summary>Human-readable title shown at the top of the chat panel.</summary>
    public string Title => ScopeType switch
    {
        ChatScopeType.Event => "Event Chat",
        ChatScopeType.Group => ContextName ?? "Group Chat",
        ChatScopeType.Direct => "Direct Chat",
        ChatScopeType.Support => "Support Chat",
        _ => "Chat",
    };

    public string Subtitle => ContextSummary ?? "Messages are delivered in real time.";

    public static ChatViewModel ForEvent(Guid eventId, IEnumerable<ChatMessageDto> history, string hubUrl, string accessToken) =>
        new()
        {
            ScopeType = ChatScopeType.Event,
            ScopeId = eventId,
            History = history.ToList(),
            HubUrl = hubUrl,
            AccessToken = accessToken,
        };

    public static ChatViewModel ForGroup(
        Guid groupId,
        IEnumerable<ChatMessageDto> history,
        string hubUrl,
        string accessToken,
        string? groupName = null,
        int? activeMemberCount = null) =>
        new()
        {
            ScopeType = ChatScopeType.Group,
            ScopeId = groupId,
            History = history.ToList(),
            HubUrl = hubUrl,
            AccessToken = accessToken,
            ContextName = groupName,
            ContextSummary = activeMemberCount.HasValue
                ? $"{activeMemberCount.Value} active {(activeMemberCount.Value == 1 ? "member" : "members")} can read and send messages here."
                : "Messages are delivered in real time to current group members.",
            BackLinkUrl = $"/Group/Manage?groupId={groupId}",
            BackLinkText = "Back to Group",
        };

    public static ChatViewModel ForDirect(Guid directChatId, IEnumerable<ChatMessageDto> history, string hubUrl, string accessToken) =>
        new()
        {
            ScopeType = ChatScopeType.Direct,
            ScopeId = directChatId,
            History = history.ToList(),
            HubUrl = hubUrl,
            AccessToken = accessToken,
        };

    public static ChatViewModel ForSupport(Guid userId, IEnumerable<ChatMessageDto> history, string hubUrl, string accessToken) =>
        new()
        {
            ScopeType = ChatScopeType.Support,
            ScopeId = userId,
            History = history.ToList(),
            HubUrl = hubUrl,
            AccessToken = accessToken,
        };
}
