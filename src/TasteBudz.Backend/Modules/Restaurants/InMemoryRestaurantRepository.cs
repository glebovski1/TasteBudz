// In-memory restaurant catalog repository backed by the seeded store.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Reads restaurants and ZIP centroids from the shared in-memory store.
/// </summary>
public sealed class InMemoryRestaurantRepository(InMemoryTasteBudzStore store) : IRestaurantRepository
{
    public Task<IReadOnlyCollection<Restaurant>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Restaurants.Values
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