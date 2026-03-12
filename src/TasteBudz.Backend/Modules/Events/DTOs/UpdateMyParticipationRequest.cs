using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

public sealed class UpdateMyParticipationRequest
{
    [Required]
    public EventParticipantState? State { get; init; }
}
