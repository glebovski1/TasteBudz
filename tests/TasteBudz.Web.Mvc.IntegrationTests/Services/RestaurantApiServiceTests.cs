using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class RestaurantApiServiceTests
{
    [Fact]
    public async Task BrowseGetAndSuggestions_SendExpectedRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new RestaurantApiService(client));
        var restaurantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?q=ramen&cuisine=Japanese&priceTier=Three&zipCode=45220&radiusMiles=5.5&page=2&pageSize=15",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, null, 1.2),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, null, 1.2)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/suggestions?eventId={eventId}&groupId={groupId}&zipCode=45220&radiusMiles=8.5&cuisineTags=Sushi&cuisineTags=Thai",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantDto(Guid.NewGuid(), "Sushi Spot", "Cincinnati", "OH", "45220", PriceTier.Two, new[] { "Sushi" }, 39.14, -84.52, null, 2.3),
                }));

        var browse = await service.BrowseAsync(new BrowseRestaurantsQuery
        {
            Q = "ramen",
            Cuisine = "Japanese",
            PriceTier = PriceTier.Three,
            ZipCode = "45220",
            RadiusMiles = 5.5,
            Page = 2,
            PageSize = 15,
        });
        var detail = await service.GetAsync(restaurantId);
        var suggestions = await service.GetSuggestionsAsync(new RestaurantSuggestionsQuery
        {
            EventId = eventId,
            GroupId = groupId,
            ZipCode = "45220",
            RadiusMiles = 8.5,
            CuisineTags = new[] { "Sushi", "Thai" },
        });

        Assert.Single(browse.Items);
        Assert.Equal("Ramen House", detail.Name);
        Assert.Single(suggestions);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
