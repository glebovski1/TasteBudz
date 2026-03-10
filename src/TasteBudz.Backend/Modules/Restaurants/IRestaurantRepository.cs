// Persistence boundary for the restaurant catalog and ZIP-code coordinate lookup.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Stores the searchable restaurant catalog and approximate ZIP centroids.
/// </summary>
public interface IRestaurantRepository
{
    Task<IReadOnlyCollection<Restaurant>> ListAsync(CancellationToken cancellationToken = default);

    Task<Restaurant?> GetAsync(Guid restaurantId, CancellationToken cancellationToken = default);

    Task<(double Latitude, double Longitude)?> GetZipCoordinatesAsync(string zipCode, CancellationToken cancellationToken = default);
}