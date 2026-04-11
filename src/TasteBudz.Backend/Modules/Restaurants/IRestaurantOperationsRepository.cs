using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Persistence boundary for restaurant-admin assignments, slots, reservations, and discount simulation state.
/// </summary>
public interface IRestaurantOperationsRepository
{
    Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<RestaurantAdminAssignment?> GetAssignmentAsync(Guid restaurantId, Guid userId, CancellationToken cancellationToken = default);

    Task SaveAssignmentAsync(RestaurantAdminAssignment assignment, CancellationToken cancellationToken = default);

    Task SaveRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RestaurantSlot>> ListSlotsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default);

    Task<RestaurantSlot?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default);

    Task SaveSlotAsync(RestaurantSlot slot, CancellationToken cancellationToken = default);

    Task<EventSlotReservation?> GetActiveReservationForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<EventSlotReservation?> GetActiveReservationForSlotAsync(Guid slotId, CancellationToken cancellationToken = default);

    Task SaveReservationAsync(EventSlotReservation reservation, CancellationToken cancellationToken = default);

    Task<DiscountActivation?> GetDiscountActivationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task SaveDiscountActivationAsync(DiscountActivation activation, CancellationToken cancellationToken = default);
}
