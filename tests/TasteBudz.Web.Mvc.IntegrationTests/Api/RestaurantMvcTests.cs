using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class RestaurantMvcTests
{
    [Fact]
    public async Task Index_BrowsesRestaurantsAndRendersAuthenticatedNavEntry()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?q=ramen&cuisine=Japanese&priceTier=Two&zipCode=45220&radiusMiles=10&page=2&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(
                            restaurantId,
                            "Ramen House",
                            "Cincinnati",
                            "OH",
                            "45220",
                            PriceTier.Two,
                            new[] { "Japanese", "Ramen" },
                            39.14,
                            -84.51,
                            "osm:123",
                            1.2,
                            "123 Noodle St"),
                    },
                    51)));

        using var response = await client.GetAsync("/Restaurant?q=ramen&cuisine=Japanese&priceTier=Two&zipCode=45220&radiusMiles=10&page=2");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Restaurants", html);
        Assert.Contains("Ramen House", html);
        Assert.Contains("123 Noodle St", html);
        Assert.Contains("Japanese", html);
        Assert.Contains("1.2 mi", html);
        Assert.Contains("href=\"/Restaurant\"", html);
        Assert.Contains("href=\"/Event/CreateEvent\"", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Layout_ForRestaurantAdminDistinguishesBrowseAndManagementLinks()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.RestaurantAdmin });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(Array.Empty<RestaurantDto>(), 0)));

        using var response = await client.GetAsync("/Restaurant");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"/Restaurant\"", html);
        Assert.Contains("Manage Restaurants", html);
        Assert.Contains("href=\"/RestaurantAdmin\"", html);
        factory.BackendHandler.AssertDrained();
    }
}
