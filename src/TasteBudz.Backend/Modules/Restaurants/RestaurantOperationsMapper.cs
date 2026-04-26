using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

internal static class RestaurantOperationsMapper
{
    internal static RestaurantAdminAssignmentDto ToAssignmentDto(RestaurantAdminAssignment assignment, UserAccount account) =>
        new(assignment.RestaurantId, assignment.UserId, account.Username, assignment.CreatedAtUtc);

    internal static RestaurantSlotDto ToSlotDto(RestaurantSlot slot) =>
        new(
            slot.Id,
            slot.RestaurantId,
            slot.StartsAtUtc,
            slot.EndsAtUtc,
            slot.Capacity,
            slot.CutoffAtUtc,
            slot.MinThresholdForDiscount,
            slot.DiscountPercent,
            slot.Status,
            slot.CreatedAtUtc,
            slot.UpdatedAtUtc,
            slot.CancelledAtUtc,
            slot.CancellationReason);

    internal static EventSlotReservationDto ToReservationDto(EventSlotReservation reservation, RestaurantSlot slot) =>
        new(
            reservation.Id,
            reservation.EventId,
            reservation.SlotId,
            slot.RestaurantId,
            reservation.Status,
            reservation.CreatedAtUtc,
            reservation.CancelledAtUtc,
            reservation.CancellationReason);

    internal static DiscountActivationDto ToDiscountDto(
        DiscountActivation activation,
        int joinedParticipantCount,
        int minThresholdForDiscount,
        int? discountPercent) =>
        new(
            activation.ReservationId,
            activation.IsActive,
            activation.IsFinalized,
            joinedParticipantCount,
            minThresholdForDiscount,
            discountPercent,
            activation.EvaluatedAtUtc);
}
