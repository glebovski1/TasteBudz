using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Payments;

public sealed class CheckoutSessionService(
    ICheckoutSessionRepository checkoutSessionRepository,
    IEventRepository eventRepository,
    IRestaurantRepository restaurantRepository,
    IFeatureFlagService featureFlagService,
    DiscountEligibilityService discountEligibilityService,
    IClock clock)
{
    private const string Currency = "USD";
    private const int DiscountPercent = 15;

    public async Task<CheckoutSessionDto> CreateForEventAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default)
    {
        EnsureCheckoutEnabled();

        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");

        if (eventRecord.Status == EventStatus.Cancelled)
        {
            throw ApiException.Conflict("Cancelled events cannot start checkout.");
        }

        var participant = await eventRepository.GetParticipantAsync(eventId, currentUser.UserId, cancellationToken);

        if (participant?.State != EventParticipantState.Joined)
        {
            throw ApiException.NotFound("The requested event could not be found.");
        }

        if (!eventRecord.SelectedRestaurantId.HasValue)
        {
            throw ApiException.Conflict("Checkout requires a selected restaurant.");
        }

        var restaurant = await restaurantRepository.GetAsync(eventRecord.SelectedRestaurantId.Value, cancellationToken)
            ?? throw ApiException.NotFound("The selected restaurant could not be found.");
        var existing = (await checkoutSessionRepository.ListForEventUserAsync(eventId, currentUser.UserId, cancellationToken))
            .FirstOrDefault(session => session.Status is CheckoutSessionStatus.Pending or CheckoutSessionStatus.Completed);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var subtotalCents = PriceTierToSubtotalCents(restaurant.PriceTier);
        var discount = await discountEligibilityService.EvaluateForEventAsync(eventId, cancellationToken);
        var discountCents = discount?.IsActive == true ? subtotalCents * DiscountPercent / 100 : 0;
        var now = clock.UtcNow;
        var session = new CheckoutSession(
            Guid.NewGuid(),
            eventId,
            currentUser.UserId,
            CheckoutSessionStatus.Pending,
            Currency,
            subtotalCents,
            discountCents,
            subtotalCents - discountCents,
            now,
            now,
            null,
            null);

        await checkoutSessionRepository.SaveAsync(session, cancellationToken);
        return ToDto(session);
    }

    public async Task<CheckoutSessionDto> CompleteAsync(CurrentUser currentUser, Guid checkoutSessionId, CancellationToken cancellationToken = default)
    {
        EnsureCheckoutEnabled();

        var session = await GetOwnedSessionAsync(currentUser, checkoutSessionId, cancellationToken);

        if (session.Status == CheckoutSessionStatus.Completed)
        {
            return ToDto(session);
        }

        if (session.Status == CheckoutSessionStatus.Cancelled)
        {
            throw ApiException.Conflict("Cancelled checkout sessions cannot be completed.");
        }

        var now = clock.UtcNow;
        var completed = session with
        {
            Status = CheckoutSessionStatus.Completed,
            UpdatedAtUtc = now,
            CompletedAtUtc = now,
        };

        await checkoutSessionRepository.SaveAsync(completed, cancellationToken);
        return ToDto(completed);
    }

    public async Task<CheckoutSessionDto> CancelAsync(CurrentUser currentUser, Guid checkoutSessionId, CancellationToken cancellationToken = default)
    {
        EnsureCheckoutEnabled();

        var session = await GetOwnedSessionAsync(currentUser, checkoutSessionId, cancellationToken);

        if (session.Status == CheckoutSessionStatus.Cancelled)
        {
            return ToDto(session);
        }

        if (session.Status == CheckoutSessionStatus.Completed)
        {
            throw ApiException.Conflict("Completed checkout sessions cannot be cancelled.");
        }

        var now = clock.UtcNow;
        var cancelled = session with
        {
            Status = CheckoutSessionStatus.Cancelled,
            UpdatedAtUtc = now,
            CancelledAtUtc = now,
        };

        await checkoutSessionRepository.SaveAsync(cancelled, cancellationToken);
        return ToDto(cancelled);
    }

    private async Task<CheckoutSession> GetOwnedSessionAsync(CurrentUser currentUser, Guid checkoutSessionId, CancellationToken cancellationToken)
    {
        var session = await checkoutSessionRepository.GetAsync(checkoutSessionId, cancellationToken)
            ?? throw ApiException.NotFound("The requested checkout session could not be found.");

        if (session.UserId != currentUser.UserId)
        {
            throw ApiException.NotFound("The requested checkout session could not be found.");
        }

        return session;
    }

    private void EnsureCheckoutEnabled()
    {
        if (!featureFlagService.IsPaymentsCheckoutEnabled())
        {
            throw ApiException.NotFound("Checkout is not enabled.");
        }
    }

    private static int PriceTierToSubtotalCents(PriceTier priceTier) =>
        priceTier switch
        {
            PriceTier.One => 1500,
            PriceTier.Two => 2500,
            PriceTier.Three => 4000,
            PriceTier.Four => 6000,
            _ => 2500,
        };

    private static CheckoutSessionDto ToDto(CheckoutSession session) =>
        new(
            session.Id,
            session.EventId,
            session.UserId,
            session.Status,
            session.Currency,
            session.SubtotalCents,
            session.DiscountCents,
            session.TotalCents,
            session.CreatedAtUtc,
            session.UpdatedAtUtc,
            session.CompletedAtUtc,
            session.CancelledAtUtc);
}
