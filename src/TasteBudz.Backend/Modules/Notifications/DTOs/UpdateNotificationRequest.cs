using System.ComponentModel.DataAnnotations;

namespace TasteBudz.Backend.Modules.Notifications;

public sealed class UpdateNotificationRequest
{
    [Required]
    public bool? Read { get; init; }
}
