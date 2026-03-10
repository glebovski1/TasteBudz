// In-memory discovery repository used until a relational persistence path is defined.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Discovery;

/// <summary>
/// Stores discovery data in dictionaries keyed by the relevant user pair.
/// </summary>
public sealed class InMemoryDiscoveryRepository(InMemoryTasteBudzStore store) : IDiscoveryRepository
{
    public Task<SwipeDecision?> GetSwipeDecisionAsync(Guid actorUserId, Guid subjectUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.SwipeDecisions.TryGetValue(ToSwipeKey(actorUserId, subjectUserId), out var decision);
            return Task.FromResult(decision);
        }
    }

    public Task SaveSwipeDecisionAsync(SwipeDecision decision, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.SwipeDecisions[ToSwipeKey(decision.ActorUserId, decision.SubjectUserId)] = decision;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<SwipeDecision>> ListSwipeDecisionsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<SwipeDecision>>(store.SwipeDecisions.Values.ToArray());
        }
    }

    public Task<BudConnection?> GetBudConnectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.BudConnections.TryGetValue(ToNormalizedPairKey(firstUserId, secondUserId), out var connection);
            return Task.FromResult(connection);
        }
    }

    public Task SaveBudConnectionAsync(BudConnection connection, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            // Bud connections are symmetric, so the pair key is normalized to a stable order.
            store.BudConnections[ToNormalizedPairKey(connection.UserOneId, connection.UserTwoId)] = connection;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<BudConnection>> ListBudConnectionsAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<BudConnection>>(store.BudConnections.Values.ToArray());
        }
    }

    private static string ToSwipeKey(Guid actorUserId, Guid subjectUserId) => $"{actorUserId:N}:{subjectUserId:N}";

    private static string ToNormalizedPairKey(Guid firstUserId, Guid secondUserId)
    {
        var ordered = new[] { firstUserId, secondUserId }.OrderBy(id => id).ToArray();
        return $"{ordered[0]:N}:{ordered[1]:N}";
    }
}