// Persistence boundary for discovery swipes and Bud connections.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Discovery;

/// <summary>
/// Stores the current discovery relationship state between users.
/// </summary>
public interface IDiscoveryRepository
{
    Task<SwipeDecision?> GetSwipeDecisionAsync(Guid actorUserId, Guid subjectUserId, CancellationToken cancellationToken = default);

    Task SaveSwipeDecisionAsync(SwipeDecision decision, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SwipeDecision>> ListSwipeDecisionsAsync(CancellationToken cancellationToken = default);

    Task<BudConnection?> GetBudConnectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);

    Task SaveBudConnectionAsync(BudConnection connection, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BudConnection>> ListBudConnectionsAsync(CancellationToken cancellationToken = default);
}