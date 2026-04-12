using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Payments;

public interface ICheckoutSessionRepository
{
    Task<CheckoutSession?> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CheckoutSession>> ListForEventUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default);
}
