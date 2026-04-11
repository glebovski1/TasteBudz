using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Events;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Recalculates the persisted discount simulation state for slot-linked events.
/// </summary>
public sealed class DiscountEligibilityService(
    IRestaurantOperationsRepository restaurantOperationsRepository,
    IEventRepository eventRepository,
    IFeatureFlagService featureFlagService,
    IClock clock)
{
    public async Task<DiscountActivationDto?> EvaluateForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (!featureFlagService.IsRestaurantsDiscountsEnabled())
        {
            return null;
        }

        var reservation = await restaurantOperationsRepository.GetActiveReservationForEventAsync(eventId, cancellationToken);
        return reservation is null ? null : await EvaluateForReservationAsync(reservation, cancellationToken);
    }

    public async Task<DiscountActivationDto?> EvaluateForReservationAsync(EventSlotReservation reservation, CancellationToken cancellationToken = default)
    {
        if (!featureFlagService.IsRestaurantsDiscountsEnabled() ||
            reservation.Status != EventSlotReservationStatus.Active)
        {
            return null;
        }

        var slot = await restaurantOperationsRepository.GetSlotAsync(reservation.SlotId, cancellationToken);

        if (slot?.MinThresholdForDiscount is not int threshold)
        {
            return null;
        }

        var participants = await eventRepository.ListParticipantsAsync(reservation.EventId, cancellationToken);
        var joinedParticipants = participants.Count(participant => participant.State == EventParticipantState.Joined);
        var existing = await restaurantOperationsRepository.GetDiscountActivationAsync(reservation.Id, cancellationToken);

        if (existing?.IsFinalized == true)
        {
            return RestaurantOperationsMapper.ToDiscountDto(existing, joinedParticipants, threshold);
        }

        var now = clock.UtcNow;
        var isFinalized = now > slot.CutoffAtUtc;
        var isActive = isFinalized
            ? existing?.IsActive == true
            : joinedParticipants >= threshold;
        var activation = new DiscountActivation(reservation.Id, isActive, isFinalized, now);

        await restaurantOperationsRepository.SaveDiscountActivationAsync(activation, cancellationToken);
        return RestaurantOperationsMapper.ToDiscountDto(activation, joinedParticipants, threshold);
    }
}
