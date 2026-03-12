using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Discovery;

public sealed class RecordSwipeDecisionRequest
{
    [Required]
    public Guid? SubjectUserId { get; init; }

    [Required]
    public SwipeDecisionType? Decision { get; init; }
}
