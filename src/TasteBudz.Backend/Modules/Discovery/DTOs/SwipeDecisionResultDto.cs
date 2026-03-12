using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Discovery;

public sealed record SwipeDecisionResultDto(
    Guid SubjectUserId,
    SwipeDecisionType Decision,
    bool IsBudMatch,
    Guid? BudConnectionId);
