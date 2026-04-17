using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Messaging;

public sealed class SendSupportMessageRequest
{
    [Required]
    [MaxLength(500)]
    public string Body { get; init; } = string.Empty;
}
