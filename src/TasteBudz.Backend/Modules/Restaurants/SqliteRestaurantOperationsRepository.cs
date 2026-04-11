using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// SQLite-backed repository for restaurant operations that are separate from catalog browse.
/// </summary>
public sealed class SqliteRestaurantOperationsRepository(TasteBudzDbContext dbContext) : IRestaurantOperationsRepository
{
    public async Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        (await dbContext.RestaurantAdminAssignments
            .AsNoTracking()
            .Where(assignment => assignment.RestaurantId == restaurantId && assignment.RevokedAtUtc == null)
            .ToListAsync(cancellationToken))
        .Select(MapAssignment)
        .OrderBy(assignment => assignment.CreatedAtUtc)
        .ToArray();

    public async Task<IReadOnlyCollection<RestaurantAdminAssignment>> ListAssignmentsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.RestaurantAdminAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.RevokedAtUtc == null)
            .ToListAsync(cancellationToken))
        .Select(MapAssignment)
        .OrderBy(assignment => assignment.CreatedAtUtc)
        .ToArray();

    public async Task<RestaurantAdminAssignment?> GetAssignmentAsync(Guid restaurantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RestaurantAdminAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(assignment => assignment.RestaurantId == restaurantId && assignment.UserId == userId, cancellationToken);
        return entity is null ? null : MapAssignment(entity);
    }

    public async Task SaveAssignmentAsync(RestaurantAdminAssignment assignment, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RestaurantAdminAssignments
            .FirstOrDefaultAsync(item => item.RestaurantId == assignment.RestaurantId && item.UserId == assignment.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.RestaurantAdminAssignments.Add(ToEntity(assignment));
        }
        else
        {
            entity.CreatedAtUtc = assignment.CreatedAtUtc;
            entity.RevokedAtUtc = assignment.RevokedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Restaurants.FirstOrDefaultAsync(item => item.Id == restaurant.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.Restaurants.Add(new RestaurantEntity
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                City = restaurant.City,
                State = restaurant.State,
                ZipCode = restaurant.ZipCode,
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                PriceTier = restaurant.PriceTier,
                ExternalPlaceId = restaurant.ExternalPlaceId,
            });
        }
        else
        {
            entity.Name = restaurant.Name;
            entity.City = restaurant.City;
            entity.State = restaurant.State;
            entity.ZipCode = restaurant.ZipCode;
            entity.Latitude = restaurant.Latitude;
            entity.Longitude = restaurant.Longitude;
            entity.PriceTier = restaurant.PriceTier;
            entity.ExternalPlaceId = restaurant.ExternalPlaceId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RestaurantSlot>> ListSlotsForRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        (await dbContext.RestaurantSlots
            .AsNoTracking()
            .Where(slot => slot.RestaurantId == restaurantId)
            .ToListAsync(cancellationToken))
        .Select(MapSlot)
        .OrderBy(slot => slot.StartsAtUtc)
        .ThenBy(slot => slot.Id)
        .ToArray();

    public async Task<RestaurantSlot?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RestaurantSlots.AsNoTracking().FirstOrDefaultAsync(slot => slot.Id == slotId, cancellationToken);
        return entity is null ? null : MapSlot(entity);
    }

    public async Task SaveSlotAsync(RestaurantSlot slot, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RestaurantSlots.FirstOrDefaultAsync(item => item.Id == slot.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.RestaurantSlots.Add(ToEntity(slot));
        }
        else
        {
            entity.RestaurantId = slot.RestaurantId;
            entity.StartsAtUtc = slot.StartsAtUtc;
            entity.EndsAtUtc = slot.EndsAtUtc;
            entity.Capacity = slot.Capacity;
            entity.CutoffAtUtc = slot.CutoffAtUtc;
            entity.MinThresholdForDiscount = slot.MinThresholdForDiscount;
            entity.Status = slot.Status;
            entity.CreatedAtUtc = slot.CreatedAtUtc;
            entity.UpdatedAtUtc = slot.UpdatedAtUtc;
            entity.CancelledAtUtc = slot.CancelledAtUtc;
            entity.CancellationReason = slot.CancellationReason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventSlotReservation?> GetActiveReservationForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventSlotReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation => reservation.EventId == eventId && reservation.Status == EventSlotReservationStatus.Active, cancellationToken);
        return entity is null ? null : MapReservation(entity);
    }

    public async Task<EventSlotReservation?> GetActiveReservationForSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventSlotReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation => reservation.SlotId == slotId && reservation.Status == EventSlotReservationStatus.Active, cancellationToken);
        return entity is null ? null : MapReservation(entity);
    }

    public async Task SaveReservationAsync(EventSlotReservation reservation, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventSlotReservations.FirstOrDefaultAsync(item => item.Id == reservation.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.EventSlotReservations.Add(ToEntity(reservation));
        }
        else
        {
            entity.EventId = reservation.EventId;
            entity.SlotId = reservation.SlotId;
            entity.Status = reservation.Status;
            entity.CreatedAtUtc = reservation.CreatedAtUtc;
            entity.CancelledAtUtc = reservation.CancelledAtUtc;
            entity.CancellationReason = reservation.CancellationReason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DiscountActivation?> GetDiscountActivationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DiscountActivations.AsNoTracking().FirstOrDefaultAsync(item => item.ReservationId == reservationId, cancellationToken);
        return entity is null ? null : MapDiscountActivation(entity);
    }

    public async Task SaveDiscountActivationAsync(DiscountActivation activation, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DiscountActivations.FirstOrDefaultAsync(item => item.ReservationId == activation.ReservationId, cancellationToken);

        if (entity is null)
        {
            dbContext.DiscountActivations.Add(ToEntity(activation));
        }
        else
        {
            entity.IsActive = activation.IsActive;
            entity.IsFinalized = activation.IsFinalized;
            entity.EvaluatedAtUtc = activation.EvaluatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RestaurantAdminAssignment MapAssignment(RestaurantAdminAssignmentEntity entity) =>
        new(entity.RestaurantId, entity.UserId, entity.CreatedAtUtc, entity.RevokedAtUtc);

    private static RestaurantSlot MapSlot(RestaurantSlotEntity entity) =>
        new(
            entity.Id,
            entity.RestaurantId,
            entity.StartsAtUtc,
            entity.EndsAtUtc,
            entity.Capacity,
            entity.CutoffAtUtc,
            entity.MinThresholdForDiscount,
            entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CancelledAtUtc,
            entity.CancellationReason);

    private static EventSlotReservation MapReservation(EventSlotReservationEntity entity) =>
        new(
            entity.Id,
            entity.EventId,
            entity.SlotId,
            entity.Status,
            entity.CreatedAtUtc,
            entity.CancelledAtUtc,
            entity.CancellationReason);

    private static DiscountActivation MapDiscountActivation(DiscountActivationEntity entity) =>
        new(entity.ReservationId, entity.IsActive, entity.IsFinalized, entity.EvaluatedAtUtc);

    private static RestaurantAdminAssignmentEntity ToEntity(RestaurantAdminAssignment assignment) =>
        new()
        {
            RestaurantId = assignment.RestaurantId,
            UserId = assignment.UserId,
            CreatedAtUtc = assignment.CreatedAtUtc,
            RevokedAtUtc = assignment.RevokedAtUtc,
        };

    private static RestaurantSlotEntity ToEntity(RestaurantSlot slot) =>
        new()
        {
            Id = slot.Id,
            RestaurantId = slot.RestaurantId,
            StartsAtUtc = slot.StartsAtUtc,
            EndsAtUtc = slot.EndsAtUtc,
            Capacity = slot.Capacity,
            CutoffAtUtc = slot.CutoffAtUtc,
            MinThresholdForDiscount = slot.MinThresholdForDiscount,
            Status = slot.Status,
            CreatedAtUtc = slot.CreatedAtUtc,
            UpdatedAtUtc = slot.UpdatedAtUtc,
            CancelledAtUtc = slot.CancelledAtUtc,
            CancellationReason = slot.CancellationReason,
        };

    private static EventSlotReservationEntity ToEntity(EventSlotReservation reservation) =>
        new()
        {
            Id = reservation.Id,
            EventId = reservation.EventId,
            SlotId = reservation.SlotId,
            Status = reservation.Status,
            CreatedAtUtc = reservation.CreatedAtUtc,
            CancelledAtUtc = reservation.CancelledAtUtc,
            CancellationReason = reservation.CancellationReason,
        };

    private static DiscountActivationEntity ToEntity(DiscountActivation activation) =>
        new()
        {
            ReservationId = activation.ReservationId,
            IsActive = activation.IsActive,
            IsFinalized = activation.IsFinalized,
            EvaluatedAtUtc = activation.EvaluatedAtUtc,
        };
}
