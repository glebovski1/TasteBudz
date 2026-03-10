// Unit tests for the restaurant catalog browse filters.
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.UnitTests.Restaurants;

/// <summary>
/// Verifies cuisine and distance filtering against the seeded catalog.
/// </summary>
public sealed class RestaurantSearchServiceTests
{
    [Fact]
    public async Task BrowseAsync_FiltersSeededCatalogByCuisineAndDistance()
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var service = new RestaurantSearchService(new InMemoryRestaurantRepository(store));

        var result = await service.BrowseAsync(new BrowseRestaurantsQuery
        {
            Cuisine = "Sushi",
            ZipCode = "45220",
            RadiusMiles = 5,
            PageSize = 10,
        });
        var restaurant = Assert.Single(result.Items);

        Assert.Equal("Maki Social", restaurant.Name);
        Assert.NotNull(restaurant.DistanceMiles);
    }
}