using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Owns restaurant-admin slot creation, update, listing, and cancellation workflows.
/// </summary>
public sealed class RestaurantSlotService(
    IRestaurantRepository restaurantRepository,
    IRestaurantOperationsRepository restaurantOperationsRepository,
    IEventRepository eventRepository,
    INotificationService notificationService,
    ManagedRestaurantService managedRestaurantService,
    IClock clock,
    IPersistenceTransactionRunner? transactionRunner = null)
{
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public async Task<IReadOnlyCollection<RestaurantSlotDto>> ListReservableAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        _ = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        var now = clock.UtcNow;
        var slots = await restaurantOperationsRepository.ListSlotsForRestaurantAsync(restaurantId, cancellationToken);
        var results = new List<RestaurantSlotDto>();

        foreach (var slot in slots.Where(slot => slot.Status == RestaurantSlotStatus.Open && slot.CutoffAtUtc >= now))
        {
            if (await restaurantOperationsRepository.GetActiveReservationForSlotAsync(slot.Id, cancellationToken) is null)
            {
                results.Add(RestaurantOperationsMapper.ToSlotDto(slot));
            }
        }

        return results.OrderBy(slot => slot.StartsAtUtc).ThenBy(slot => slot.SlotId).ToArray();
    }

    public async Task<IReadOnlyCollection<RestaurantSlotDto>> ListManagedAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        CancellationToken cancellationToken = default)
    {
        await managedRestaurantService.EnsureCanManageRestaurantAsync(currentUser, restaurantId, cancellationToken);
        var slots = await restaurantOperationsRepository.ListSlotsForRestaurantAsync(restaurantId, cancellationToken);
        return slots.Select(RestaurantOperationsMapper.ToSlotDto).ToArray();
    }

    public async Task<RestaurantSlotDto> CreateAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        CreateRestaurantSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        await managedRestaurantService.EnsureCanManageRestaurantAsync(currentUser, restaurantId, cancellationToken);

        var startsAt = request.StartsAtUtc ?? throw ApiException.BadRequest("startsAtUtc is required.");
        var endsAt = request.EndsAtUtc ?? throw ApiException.BadRequest("endsAtUtc is required.");
        var capacity = request.Capacity ?? throw ApiException.BadRequest("capacity is required.");
        var cutoffAt = request.CutoffAtUtc ?? throw ApiException.BadRequest("cutoffAtUtc is required.");
        ValidateSlot(startsAt, endsAt, capacity, cutoffAt, request.MinThresholdForDiscount, request.DiscountPercent);

        var now = clock.UtcNow;
        var slot = new RestaurantSlot(
            Guid.NewGuid(),
            restaurantId,
            startsAt,
            endsAt,
            capacity,
            cutoffAt,
            request.MinThresholdForDiscount,
            request.DiscountPercent,
            RestaurantSlotStatus.Open,
            now,
            now,
            null,
            null);

        await restaurantOperationsRepository.SaveSlotAsync(slot, cancellationToken);
        return RestaurantOperationsMapper.ToSlotDto(slot);
    }

    public async Task<RestaurantSlotDto> UpdateAsync(
        CurrentUser currentUser,
        Guid slotId,
        UpdateRestaurantSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var slot = await restaurantOperationsRepository.GetSlotAsync(slotId, cancellationToken)
            ?? throw ApiException.NotFound("The requested slot could not be found.");

        await managedRestaurantService.EnsureCanManageRestaurantAsync(currentUser, slot.RestaurantId, cancellationToken);

        if (slot.Status == RestaurantSlotStatus.Cancelled)
        {
            throw ApiException.Conflict("Cancelled slots cannot be updated.");
        }

        if (await restaurantOperationsRepository.GetActiveReservationForSlotAsync(slot.Id, cancellationToken) is not null)
        {
            throw ApiException.Conflict("Reserved slots cannot be updated.");
        }

        var minThresholdForDiscount = request.ClearDiscount
            ? null
            : request.MinThresholdForDiscount ?? slot.MinThresholdForDiscount;
        var discountPercent = request.ClearDiscount
            ? null
            : request.DiscountPercent ?? slot.DiscountPercent;

        if (request.ClearDiscount && (request.MinThresholdForDiscount.HasValue || request.DiscountPercent.HasValue))
        {
            throw ApiException.BadRequest("clearDiscount cannot be combined with discount fields.");
        }

        var updated = slot with
        {
            StartsAtUtc = request.StartsAtUtc ?? slot.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc ?? slot.EndsAtUtc,
            Capacity = request.Capacity ?? slot.Capacity,
            CutoffAtUtc = request.CutoffAtUtc ?? slot.CutoffAtUtc,
            MinThresholdForDiscount = minThresholdForDiscount,
            DiscountPercent = discountPercent,
            UpdatedAtUtc = clock.UtcNow,
        };

        ValidateSlot(updated.StartsAtUtc, updated.EndsAtUtc, updated.Capacity, updated.CutoffAtUtc, updated.MinThresholdForDiscount, updated.DiscountPercent);
        await restaurantOperationsRepository.SaveSlotAsync(updated, cancellationToken);
        return RestaurantOperationsMapper.ToSlotDto(updated);
    }

    public async Task CancelAsync(
        CurrentUser currentUser,
        Guid slotId,
        CancelRestaurantSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var slot = await restaurantOperationsRepository.GetSlotAsync(slotId, cancellationToken)
            ?? throw ApiException.NotFound("The requested slot could not be found.");

        await managedRestaurantService.EnsureCanManageRestaurantAsync(currentUser, slot.RestaurantId, cancellationToken);

        if (slot.Status == RestaurantSlotStatus.Cancelled)
        {
            return;
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? throw ApiException.BadRequest("reason is required.")
            : request.Reason.Trim();
        var now = clock.UtcNow;
        var cancelledSlot = slot with
        {
            Status = RestaurantSlotStatus.Cancelled,
            UpdatedAtUtc = now,
            CancelledAtUtc = now,
            CancellationReason = reason,
        };

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await restaurantOperationsRepository.SaveSlotAsync(cancelledSlot, cancellationToken);
                var reservation = await restaurantOperationsRepository.GetActiveReservationForSlotAsync(slot.Id, cancellationToken);

                if (reservation is not null)
                {
                    var cancelledReservation = reservation with
                    {
                        Status = EventSlotReservationStatus.Cancelled,
                        CancelledAtUtc = now,
                        CancellationReason = reason,
                    };
                    await restaurantOperationsRepository.SaveReservationAsync(cancelledReservation, cancellationToken);
                    await CancelLinkedEventAsync(reservation.EventId, reason, now, cancellationToken);
                }
            },
            cancellationToken);
    }

    private async Task CancelLinkedEventAsync(Guid eventId, string reason, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken);

        if (eventRecord is null || eventRecord.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            return;
        }

        var cancelled = eventRecord with
        {
            Status = EventStatus.Cancelled,
            CancellationReason = $"Restaurant slot cancelled: {reason}",
            CancelledAtUtc = now,
            UpdatedAtUtc = now,
        };

        await eventRepository.SaveAsync(cancelled, cancellationToken);
        var participants = await eventRepository.ListParticipantsAsync(eventId, cancellationToken);
        var recipientIds = participants
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .Select(participant => participant.UserId)
            .Where(userId => userId != cancelled.HostUserId)
            .Append(cancelled.HostUserId)
            .Distinct()
            .ToArray();

        foreach (var recipientId in recipientIds)
        {
            await notificationService.CreateAsync(
                new Notification(Guid.NewGuid(), recipientId, NotificationType.EventCancelled, "Event", cancelled.Id, cancelled.CancellationReason, now, null),
                cancellationToken);
        }
    }

    private static void ValidateSlot(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int capacity,
        DateTimeOffset cutoffAt,
        int? minThresholdForDiscount,
        int? discountPercent)
    {
        if (endsAt <= startsAt)
        {
            throw ApiException.BadRequest("endsAtUtc must be after startsAtUtc.");
        }

        if (cutoffAt > startsAt)
        {
            throw ApiException.BadRequest("cutoffAtUtc must be before or equal to startsAtUtc.");
        }

        if (capacity is < 2 or > 8)
        {
            throw ApiException.BadRequest("capacity must be between 2 and 8.");
        }

        if (minThresholdForDiscount.HasValue && (minThresholdForDiscount.Value < 2 || minThresholdForDiscount.Value > capacity))
        {
            throw ApiException.BadRequest("minThresholdForDiscount must be between 2 and capacity.");
        }

        if (minThresholdForDiscount.HasValue != discountPercent.HasValue)
        {
            throw ApiException.BadRequest("minThresholdForDiscount and discountPercent must be provided together.");
        }

        if (discountPercent.HasValue && discountPercent.Value is < 1 or > 100)
        {
            throw ApiException.BadRequest("discountPercent must be between 1 and 100.");
        }
    }
}
