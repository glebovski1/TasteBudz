using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// SQLite-backed repository for the seeded restaurant catalog and ZIP lookup data.
/// </summary>
public sealed class SqliteRestaurantRepository(TasteBudzDbContext dbContext) : IRestaurantRepository
{
    public async Task<IReadOnlyCollection<Restaurant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var restaurants = await dbContext.Restaurants.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
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
            entity.ExternalPlaceId);
}
