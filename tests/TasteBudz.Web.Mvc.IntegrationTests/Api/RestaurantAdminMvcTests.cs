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
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
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
        Assert.Contains("Update Slot", manageHtml);
        Assert.Contains("20% at 2 guests", manageHtml);
        Assert.Contains(slotId.ToString(), manageHtml);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task UpdateSlot_PostsEditedSlotToBackend()
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
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }));

        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/RestaurantAdmin/Manage?restaurantId={restaurantId}");

        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/restaurant-admin/slots/{slotId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 30, 0, TimeSpan.Zero), 6, new DateTimeOffset(2026, 5, 1, 17, 30, 0, TimeSpan.Zero), 4, 25, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null)));

        using var response = await client.PostAsync(
            "/RestaurantAdmin/UpdateSlot",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["RestaurantId"] = restaurantId.ToString(),
                ["SlotId"] = slotId.ToString(),
                ["StartsAt"] = "2026-05-01T18:30",
                ["EndsAt"] = "2026-05-01T22:30",
                ["Capacity"] = "6",
                ["CutoffAt"] = "2026-05-01T17:30",
                ["MinThresholdForDiscount"] = "4",
                ["DiscountPercent"] = "25",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "\"capacity\":6",
            factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}").Body);
        Assert.Contains(
            "\"minThresholdForDiscount\":4",
            factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}").Body);
        Assert.Contains(
            "\"discountPercent\":25",
            factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}").Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task UpdateSlot_WithBlankDiscountFields_PostsClearDiscountToBackend()
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
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }));

        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/RestaurantAdmin/Manage?restaurantId={restaurantId}");

        factory.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/restaurant-admin/slots/{slotId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 30, 0, TimeSpan.Zero), 6, new DateTimeOffset(2026, 5, 1, 17, 30, 0, TimeSpan.Zero), null, null, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null)));

        using var response = await client.PostAsync(
            "/RestaurantAdmin/UpdateSlot",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["RestaurantId"] = restaurantId.ToString(),
                ["SlotId"] = slotId.ToString(),
                ["StartsAt"] = "2026-05-01T18:30",
                ["EndsAt"] = "2026-05-01T22:30",
                ["Capacity"] = "6",
                ["CutoffAt"] = "2026-05-01T17:30",
                ["MinThresholdForDiscount"] = "",
                ["DiscountPercent"] = "",
            }));

        var body = factory.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}").Body;
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("\"clearDiscount\":true", body);
        Assert.DoesNotContain("\"minThresholdForDiscount\"", body);
        Assert.DoesNotContain("\"discountPercent\"", body);
        factory.BackendHandler.AssertDrained();
    }
}
