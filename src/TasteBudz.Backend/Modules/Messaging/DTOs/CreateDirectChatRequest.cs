using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Messaging;

public sealed class CreateDirectChatRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }
}
