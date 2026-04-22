using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AdminRestaurantCatalogMvcTests
{
    [Fact]
    public async Task RestaurantCatalogPage_RendersCreateAndEditForms()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new AdminRestaurantCatalogItemDto(
                        restaurantId,
                        "Ramen House",
                        "123 Elm St",
                        "Cincinnati",
                        "OH",
                        "45220",
                        PriceTier.Three,
                        new[] { "Japanese", "Sushi" },
                        39.14,
                        -84.51,
                        "osm:node:123",
                        false),
                }));

        using var response = await client.GetAsync("/Admin/Restaurants");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Create Restaurant", html);
        Assert.Contains("Ramen House", html);
        Assert.Contains("123 Elm St", html);
        Assert.Contains("Cuisine Tags", html);
        factory.BackendHandler.AssertDrained();
    }
}
