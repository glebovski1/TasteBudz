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
        var slotStart = DateTimeOffset.UtcNow.AddDays(5);

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
                    new RestaurantSlotDto(slotId, restaurantId, slotStart, slotStart.AddHours(4), 4, slotStart.AddHours(-1), 2, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
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
        Assert.Contains("Edit", manageHtml);
        Assert.DoesNotContain("slot-form-grid", manageHtml);
        Assert.Contains("20% at 2 guests", manageHtml);
        Assert.Contains(slotId.ToString(), manageHtml);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Manage_PaginatesNextMonthSlotsAndRendersOneEditPanel()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantId = Guid.NewGuid();
        var slots = Enumerable.Range(1, 11)
            .Select(index =>
            {
                var startsAt = DateTimeOffset.UtcNow.AddDays(index);
                return new RestaurantSlotDto(
                    Guid.NewGuid(),
                    restaurantId,
                    startsAt,
                    startsAt.AddHours(2),
                    4,
                    startsAt.AddHours(-1),
                    index == 11 ? 2 : null,
                    index == 11 ? 15 : null,
                    RestaurantSlotStatus.Open,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null,
                    null);
            })
            .ToArray();
        var outsideWindowSlotId = Guid.NewGuid();
        var outsideWindowStart = DateTimeOffset.UtcNow.AddDays(40);
        var editSlot = slots[^1];

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
                slots.Concat(new[]
                {
                    new RestaurantSlotDto(outsideWindowSlotId, restaurantId, outsideWindowStart, outsideWindowStart.AddHours(2), 4, outsideWindowStart.AddHours(-1), 2, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }).ToArray()));

        using var response = await client.GetAsync($"/RestaurantAdmin/Manage?restaurantId={restaurantId}&slotPage=2&slotStatus=Open&editSlotId={editSlot.SlotId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Page 2 of 2", html);
        Assert.Contains(editSlot.SlotId.ToString(), html);
        Assert.Contains("Update Slot", html);
        Assert.Contains("15% at 2 guests", html);
        Assert.DoesNotContain(slots[0].SlotId.ToString(), html);
        Assert.DoesNotContain(outsideWindowSlotId.ToString(), html);
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
