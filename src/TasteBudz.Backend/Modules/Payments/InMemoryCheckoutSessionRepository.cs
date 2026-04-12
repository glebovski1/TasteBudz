using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Payments;

public sealed class InMemoryCheckoutSessionRepository(InMemoryTasteBudzStore store) : ICheckoutSessionRepository
{
    public Task<CheckoutSession?> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.CheckoutSessions.TryGetValue(checkoutSessionId, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<IReadOnlyCollection<CheckoutSession>> ListForEventUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var sessions = store.CheckoutSessions.Values
                .Where(session => session.EventId == eventId && session.UserId == userId)
                .OrderByDescending(session => session.CreatedAtUtc)
                .ThenByDescending(session => session.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<CheckoutSession>>(sessions);
        }
    }

    public Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.CheckoutSessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
