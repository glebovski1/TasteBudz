using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class RestaurantAdminMvcTests
{
    [Fact]
    public async Task RestaurantAdminPages_RenderManagedRestaurantAndSlots()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.RestaurantAdmin });
        factory.BackendHandler.Requests.Clear();

        var restaurant = new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, null, null);
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurant-admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new[] { restaurant }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurant-admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new[] { restaurant }));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }));

        using var indexResponse = await client.GetAsync("/RestaurantAdmin");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        using var manageResponse = await client.GetAsync($"/RestaurantAdmin/Manage?restaurantId={restaurantId}");
        var manageHtml = await manageResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, indexResponse.StatusCode);
        Assert.Contains("Ramen House", indexHtml);
        Assert.Equal(HttpStatusCode.OK, manageResponse.StatusCode);
        Assert.Contains("Restaurant Profile", manageHtml);
        Assert.Contains("Create Slot", manageHtml);
        Assert.Contains(slotId.ToString(), manageHtml);
        factory.BackendHandler.AssertDrained();
    }
}
