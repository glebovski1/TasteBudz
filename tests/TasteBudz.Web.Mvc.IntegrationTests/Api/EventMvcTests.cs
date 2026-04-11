using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class EventMvcTests
{
    [Fact]
    public async Task CreateEvent_RendersGoogleMapsLinksForRestaurantPins()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantWithPlaceId = Guid.NewGuid();
        var restaurantWithoutPlaceId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(restaurantWithPlaceId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, "google-place-123", 1.2),
                        new RestaurantDto(restaurantWithoutPlaceId, "Taco Corner", "Cincinnati", "OH", "45202", PriceTier.One, new[] { "Mexican" }, 39.10, -84.50, null, 2.4),
                        new RestaurantDto(Guid.NewGuid(), "OpenStreetMap Bistro", "Cincinnati", "OH", "45206", PriceTier.Two, new[] { "American" }, 39.13, -84.48, "osm:987654321", 2.8),
                        new RestaurantDto(Guid.NewGuid(), "<img src=x onerror=alert(1)>", "Cincinnati", "OH", "45206", PriceTier.Two, new[] { "American" }, 39.12, -84.47, null, 3.1),
                    },
                    4)));

        using var response = await client.GetAsync("/Event/CreateEvent");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Open in Google Maps", html);
        Assert.Contains("query_place_id=google-place-123", html);
        Assert.Contains("Taco%20Corner%2C%20Cincinnati%2C%20OH%2045202", html);
        Assert.Contains("OpenStreetMap%20Bistro%2C%20Cincinnati%2C%20OH%2045206", html);
        Assert.DoesNotContain("onclick=\"window.selectRestaurant", html);
        Assert.DoesNotContain("<img src=x onerror=alert(1)>", html);
        Assert.DoesNotContain("query_place_id=osm", html);
        Assert.DoesNotContain("query_place_id=osm%3A", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_WhenRestaurantSelected_RendersGoogleMapsLink()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Friday Ramen",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    6,
                    2,
                    1,
                    hostUserId,
                    restaurantId,
                    null,
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventParticipantDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, "google-place-123", 1.2)));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Friday Ramen", html);
        Assert.Contains("Ramen House", html);
        Assert.Contains("Open in Google Maps", html);
        Assert.Contains("query_place_id=google-place-123", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task EventDetails_ForHostWithReservableSlots_RendersReservationAction()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(
                    eventId,
                    "Friday Ramen",
                    EventType.Open,
                    EventStatus.Open,
                    new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                    4,
                    2,
                    1,
                    session.CurrentUser.UserId,
                    restaurantId,
                    null,
                    null,
                    null)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<EventParticipantDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Ramen House", "Cincinnati", "OH", "45220", PriceTier.Three, new[] { "Japanese" }, 39.14, -84.51, null, 1.2)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new RestaurantSlotDto(slotId, restaurantId, new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero), 4, new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero), 2, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null),
                }));

        using var response = await client.GetAsync($"/Event/EventDetails?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reserve a Slot", html);
        Assert.Contains("Reserve", html);
        Assert.Contains(slotId.ToString(), html);
        factory.BackendHandler.AssertDrained();
    }
}
