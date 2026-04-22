// In-memory restaurant catalog repository used by unit tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Reads restaurants and ZIP centroids from the shared in-memory test store.
/// </summary>
public sealed class InMemoryRestaurantRepository(InMemoryTasteBudzStore store) : IRestaurantRepository
{
    public Task<IReadOnlyCollection<Restaurant>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Restaurants.Values
                .Where(restaurant => includeArchived || !restaurant.IsArchived)
                .OrderBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Restaurant>>(items);
        }
    }

    public Task<Restaurant?> GetAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Restaurants.TryGetValue(restaurantId, out var restaurant);
            return Task.FromResult(restaurant);
        }
    }

    public Task SaveAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Restaurants[restaurant.Id] = restaurant;
            return Task.CompletedTask;
        }
    }

    public Task<(double Latitude, double Longitude)?> GetZipCoordinatesAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            if (store.ZipCoordinates.TryGetValue(zipCode.Trim(), out var coordinates))
            {
                return Task.FromResult<(double Latitude, double Longitude)?>(coordinates);
            }

            return Task.FromResult<(double Latitude, double Longitude)?>(null);
        }
    }
}
