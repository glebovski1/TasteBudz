using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// SQLite-backed repository for the seeded restaurant catalog and ZIP lookup data.
/// </summary>
public sealed class SqliteRestaurantRepository(TasteBudzDbContext dbContext) : IRestaurantRepository
{
    public async Task<IReadOnlyCollection<Restaurant>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var restaurants = await dbContext.Restaurants
            .AsNoTracking()
            .Where(item => includeArchived || !item.IsArchived)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var cuisineLinks = await dbContext.RestaurantCuisines.AsNoTracking().ToListAsync(cancellationToken);
        var cuisines = await dbContext.Cuisines.AsNoTracking().ToDictionaryAsync(item => item.Id, cancellationToken);

        return restaurants
            .Select(restaurant => MapRestaurant(
                restaurant,
                cuisineLinks.Where(link => link.RestaurantId == restaurant.Id).Select(link => cuisines[link.CuisineId].Name)))
            .ToArray();
    }

    public async Task<Restaurant?> GetAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        var restaurant = await dbContext.Restaurants.AsNoTracking().FirstOrDefaultAsync(item => item.Id == restaurantId, cancellationToken);

        if (restaurant is null)
        {
            return null;
        }

        var cuisineLinks = await dbContext.RestaurantCuisines
            .AsNoTracking()
            .Where(link => link.RestaurantId == restaurantId)
            .ToListAsync(cancellationToken);
        var cuisines = cuisineLinks.Count == 0
            ? new Dictionary<Guid, CuisineEntity>()
            : await dbContext.Cuisines.AsNoTracking().Where(item => cuisineLinks.Select(link => link.CuisineId).Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);

        return MapRestaurant(restaurant, cuisineLinks.Select(link => cuisines[link.CuisineId].Name));
    }

    public async Task<IReadOnlyCollection<Guid>> ListRestaurantIdsWithDiscountSlotsAsync(
        DateTimeOffset startsAtFromUtc,
        DateTimeOffset startsAtToUtc,
        CancellationToken cancellationToken = default)
    {
        var reservedSlotIds = await dbContext.EventSlotReservations
            .AsNoTracking()
            .Where(reservation => reservation.Status == EventSlotReservationStatus.Active)
            .Select(reservation => reservation.SlotId)
            .ToListAsync(cancellationToken);

        var reservedSlotIdSet = reservedSlotIds.ToHashSet();
        var discountSlots = await dbContext.RestaurantSlots
            .AsNoTracking()
            .Where(slot => slot.Status == RestaurantSlotStatus.Open)
            .Where(slot => slot.MinThresholdForDiscount != null && slot.DiscountPercent != null)
            .ToListAsync(cancellationToken);

        return discountSlots
            .Where(slot => slot.StartsAtUtc >= startsAtFromUtc && slot.StartsAtUtc <= startsAtToUtc)
            .Where(slot => slot.CutoffAtUtc >= startsAtFromUtc)
            .Where(slot => !reservedSlotIdSet.Contains(slot.Id))
            .Select(slot => slot.RestaurantId)
            .Distinct()
            .ToArray();
    }

    public async Task SaveAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Restaurants.FirstOrDefaultAsync(item => item.Id == restaurant.Id, cancellationToken);

        if (entity is null)
        {
            entity = new RestaurantEntity
            {
                Id = restaurant.Id,
            };

            dbContext.Restaurants.Add(entity);
        }

        entity.Name = restaurant.Name;
        entity.StreetAddress = restaurant.StreetAddress;
        entity.City = restaurant.City;
        entity.State = restaurant.State;
        entity.ZipCode = restaurant.ZipCode;
        entity.Latitude = restaurant.Latitude;
        entity.Longitude = restaurant.Longitude;
        entity.PriceTier = restaurant.PriceTier;
        entity.ExternalPlaceId = restaurant.ExternalPlaceId;
        entity.IsArchived = restaurant.IsArchived;

        var normalizedCuisineTags = restaurant.CuisineTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var requiredCuisineNames = normalizedCuisineTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cuisineEntities = requiredCuisineNames.Count == 0
            ? []
            : await dbContext.Cuisines
                .Where(item => requiredCuisineNames.Contains(item.Name))
                .ToListAsync(cancellationToken);

        foreach (var missingCuisineName in requiredCuisineNames.Except(cuisineEntities.Select(item => item.Name), StringComparer.OrdinalIgnoreCase))
        {
            var cuisine = new CuisineEntity
            {
                Id = Guid.NewGuid(),
                Name = missingCuisineName,
            };

            dbContext.Cuisines.Add(cuisine);
            cuisineEntities.Add(cuisine);
        }

        var cuisineIdsByName = cuisineEntities.ToDictionary(item => item.Name, item => item.Id, StringComparer.OrdinalIgnoreCase);
        var requiredCuisineIds = normalizedCuisineTags
            .Select(tag => cuisineIdsByName[tag])
            .ToHashSet();

        var existingLinks = await dbContext.RestaurantCuisines
            .Where(link => link.RestaurantId == restaurant.Id)
            .ToListAsync(cancellationToken);

        foreach (var link in existingLinks.Where(link => !requiredCuisineIds.Contains(link.CuisineId)))
        {
            dbContext.RestaurantCuisines.Remove(link);
        }

        var existingCuisineIds = existingLinks.Select(link => link.CuisineId).ToHashSet();

        foreach (var cuisineId in requiredCuisineIds.Where(cuisineId => !existingCuisineIds.Contains(cuisineId)))
        {
            dbContext.RestaurantCuisines.Add(new RestaurantCuisineEntity
            {
                RestaurantId = restaurant.Id,
                CuisineId = cuisineId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(double Latitude, double Longitude)?> GetZipCoordinatesAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        var value = zipCode.Trim();
        var entity = await dbContext.ZipCoordinates.AsNoTracking().FirstOrDefaultAsync(item => item.ZipCode == value, cancellationToken);
        return entity is null ? null : (entity.Latitude, entity.Longitude);
    }

    private static Restaurant MapRestaurant(RestaurantEntity entity, IEnumerable<string> cuisineTags) =>
        new(
            entity.Id,
            entity.Name,
            entity.City,
            entity.State,
            entity.ZipCode,
            entity.Latitude,
            entity.Longitude,
            entity.PriceTier,
            cuisineTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
            entity.ExternalPlaceId,
            entity.StreetAddress,
            entity.IsArchived);
}
