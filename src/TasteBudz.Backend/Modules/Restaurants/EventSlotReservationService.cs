using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Concurrency;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Events;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Links event-host workflows to restaurant-managed slots.
/// </summary>
public sealed class EventSlotReservationService(
    IEventRepository eventRepository,
    IRestaurantOperationsRepository restaurantOperationsRepository,
    EventLifecycleService lifecycleService,
    DiscountEligibilityService discountEligibilityService,
    IKeyedLockProvider keyedLockProvider,
    IClock clock,
    IPersistenceTransactionRunner? transactionRunner = null)
{
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public async Task<EventSlotReservationDto> ReserveAsync(
        CurrentUser currentUser,
        Guid eventId,
        ReserveEventSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var slotId = request.SlotId ?? throw ApiException.BadRequest("slotId is required.");

        await using var eventLock = await keyedLockProvider.AcquireAsync($"event-slot-reservation:event:{eventId:N}", cancellationToken);
        await using var slotLock = await keyedLockProvider.AcquireAsync($"event-slot-reservation:slot:{slotId:N}", cancellationToken);

        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");
        eventRecord = await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);

        if (eventRecord.HostUserId != currentUser.UserId)
        {
            throw ApiException.Forbidden("Only the event host can reserve a restaurant slot.");
        }

        if (eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw ApiException.Conflict("Only active events can reserve a restaurant slot.");
        }

        var slot = await restaurantOperationsRepository.GetSlotAsync(slotId, cancellationToken)
            ?? throw ApiException.NotFound("The requested slot could not be found.");

        if (slot.Status != RestaurantSlotStatus.Open)
        {
            throw ApiException.Conflict("Only open slots can be reserved.");
        }

        if (eventRecord.EventStartAtUtc < slot.StartsAtUtc || eventRecord.EventStartAtUtc > slot.EndsAtUtc)
        {
            throw ApiException.Conflict("The event time must fit inside the selected slot window.");
        }

        if (eventRecord.Capacity > slot.Capacity)
        {
            throw ApiException.Conflict("Event capacity cannot exceed slot capacity.");
        }

        var existingForEvent = await restaurantOperationsRepository.GetActiveReservationForEventAsync(eventId, cancellationToken);

        if (existingForEvent is not null)
        {
            if (existingForEvent.SlotId == slot.Id)
            {
                return RestaurantOperationsMapper.ToReservationDto(existingForEvent, slot);
            }

            throw ApiException.Conflict("This event already has an active slot reservation.");
        }

        if (await restaurantOperationsRepository.GetActiveReservationForSlotAsync(slot.Id, cancellationToken) is not null)
        {
            throw ApiException.Conflict("This slot is already reserved.");
        }

        var now = clock.UtcNow;
        var reservation = new EventSlotReservation(
            Guid.NewGuid(),
            eventId,
            slot.Id,
            EventSlotReservationStatus.Active,
            now,
            null,
            null);
        var updatedEvent = eventRecord with
        {
            SelectedRestaurantId = slot.RestaurantId,
            CuisineTarget = null,
            UpdatedAtUtc = now,
        };

        return await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await eventRepository.SaveAsync(updatedEvent, cancellationToken);
                await restaurantOperationsRepository.SaveReservationAsync(reservation, cancellationToken);
                await discountEligibilityService.EvaluateForReservationAsync(reservation, cancellationToken);
                return RestaurantOperationsMapper.ToReservationDto(reservation, slot);
            },
            cancellationToken);
    }
}
