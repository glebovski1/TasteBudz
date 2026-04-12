using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Messaging;

public sealed class CreateDirectChatRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }
}

public sealed class SendDirectChatMessageRequest
{
    [Required]
    [MaxLength(500)]
    public string Body { get; init; } = string.Empty;
}

public sealed record DirectChatDto(
    Guid DirectChatId,
    Guid OtherUserId,
    string OtherUsername,
    string OtherDisplayName,
    DateTimeOffset CreatedAtUtc);
