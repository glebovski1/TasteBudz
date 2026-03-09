// Request and response contracts for scoped chat history and send workflows.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// Chat message payload returned by history endpoints and hub broadcasts.
/// </summary>
public sealed record ChatMessageDto(
    Guid MessageId,
    Guid SenderUserId,
    string SenderUsername,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Query parameters for cursor-based chat history.
/// </summary>
public sealed class ChatHistoryQuery
{
    public string? Cursor { get; init; }

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Hub request body for sending one message into an authorized scope.
/// </summary>
public sealed class SendChatMessageRequest
{
    [Required]
    public ChatScopeType? ScopeType { get; init; }

    [Required]
    public Guid? ScopeId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Body { get; init; } = string.Empty;
}
