using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Messaging;

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
