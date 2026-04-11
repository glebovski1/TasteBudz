using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// In-memory restaurant operations repository used by unit tests.
/// </summary>
public sealed class InMemoryRestaurantOperationsRepository(InMemoryTasteBudzStore store) : IRestaurantOperationsRepository
{
    public Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.RestaurantAdminAssignments.Values
                .Where(assignment => assignment.RestaurantId == restaurantId && assignment.RevokedAtUtc is null)
                .OrderBy(assignment => assignment.CreatedAtUtc)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<RestaurantAdminAssignment>>(items);
        }
    }

    public Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.RestaurantAdminAssignments.Values
                .Where(assignment => assignment.UserId == userId && assignment.RevokedAtUtc is null)
                .OrderBy(assignment => assignment.CreatedAtUtc)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<RestaurantAdminAssignment>>(items);
        }
    }

    public Task<RestaurantAdminAssignment?> GetAssignmentAsync(Guid restaurantId, Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.RestaurantAdminAssignments.TryGetValue(AssignmentKey(restaurantId, userId), out var assignment);
            return Task.FromResult(assignment);
        }
    }

    public Task SaveAssignmentAsync(RestaurantAdminAssignment assignment, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.RestaurantAdminAssignments[AssignmentKey(assignment.RestaurantId, assignment.UserId)] = assignment;
            return Task.CompletedTask;
        }
    }

    public Task SaveRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Restaurants[restaurant.Id] = restaurant;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<RestaurantSlot>> ListSlotsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.RestaurantSlots.Values
                .Where(slot => slot.RestaurantId == restaurantId)
                .OrderBy(slot => slot.StartsAtUtc)
                .ThenBy(slot => slot.Id)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<RestaurantSlot>>(items);
        }
    }

    public Task<RestaurantSlot?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.RestaurantSlots.TryGetValue(slotId, out var slot);
            return Task.FromResult(slot);
        }
    }

    public Task SaveSlotAsync(RestaurantSlot slot, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.RestaurantSlots[slot.Id] = slot;
            return Task.CompletedTask;
        }
    }

    public Task<EventSlotReservation?> GetActiveReservationForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var reservation = store.EventSlotReservations.Values.FirstOrDefault(item =>
                item.EventId == eventId &&
                item.Status == EventSlotReservationStatus.Active);
            return Task.FromResult(reservation);
        }
    }

    public Task<EventSlotReservation?> GetActiveReservationForSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var reservation = store.EventSlotReservations.Values.FirstOrDefault(item =>
                item.SlotId == slotId &&
                item.Status == EventSlotReservationStatus.Active);
            return Task.FromResult(reservation);
        }
    }

    public Task SaveReservationAsync(EventSlotReservation reservation, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventSlotReservations[reservation.Id] = reservation;
            return Task.CompletedTask;
        }
    }

    public Task<DiscountActivation?> GetDiscountActivationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.DiscountActivations.TryGetValue(reservationId, out var activation);
            return Task.FromResult(activation);
        }
    }

    public Task SaveDiscountActivationAsync(DiscountActivation activation, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.DiscountActivations[activation.ReservationId] = activation;
            return Task.CompletedTask;
        }
    }

    private static string AssignmentKey(Guid restaurantId, Guid userId) => $"{restaurantId:N}:{userId:N}";
}
