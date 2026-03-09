// Scoped chat thread and message records used by event and group messaging.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Conversation container for one event, group, or later direct chat scope.
/// </summary>
public sealed record ChatThread(
    Guid Id,
    ChatScopeType ScopeType,
    Guid ScopeId,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Immutable text message inside one chat thread.
/// </summary>
public sealed record ChatMessage(
    Guid Id,
    Guid ThreadId,
    Guid SenderUserId,
    string Body,
    DateTimeOffset CreatedAtUtc);
