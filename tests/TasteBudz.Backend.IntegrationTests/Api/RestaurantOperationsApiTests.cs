using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.IntegrationTests.Api;

public sealed class RestaurantOperationsApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task Slots_WhenFeatureDisabled_ReturnNotFoundProblemDetails()
    {
        using var disabledFactory = CreateDisabledFactory();
        disabledFactory.ResetState();
        using var client = disabledFactory.CreateClient();
        var session = await ApiTestHelpers.RegisterAsync(client, username: "foodie", email: "foodie@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.GetAsync("/api/v1/restaurants/11111111-1111-1111-1111-111111111111/slots");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, problem!.Status);
    }

    [Fact]
    public async Task Slots_WithDefaultFeatureFlags_ReturnOk()
    {
        factory.ResetState();
        using var client = factory.CreateClient();
        var session = await ApiTestHelpers.RegisterAsync(client, username: "foodie", email: "foodie@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.GetAsync("/api/v1/restaurants/11111111-1111-1111-1111-111111111111/slots");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RestaurantOperations_AssignmentSlotAndReservationFlow_Succeeds()
    {
        using var enabledFactory = CreateEnabledFactory();
        enabledFactory.ResetState();
        using var client = enabledFactory.CreateClient();
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var admin = await ApiTestHelpers.RegisterAsync(client, username: "admin", email: "admin@example.com");
        var manager = await ApiTestHelpers.RegisterAsync(client, username: "manager", email: "manager@example.com");
        var host = await ApiTestHelpers.RegisterAsync(client, username: "host", email: "host@example.com");
        var guest = await ApiTestHelpers.RegisterAsync(client, username: "guest", email: "guest@example.com");
        await ApiTestHelpers.PromoteRolesAsync(enabledFactory.Services, admin.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });

        ApiTestHelpers.SetBearer(client, admin.AccessToken);
        var grantResponse = await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            new CreateRestaurantAdminAssignmentRequest { Username = "manager" },
            ApiTestHelpers.JsonOptions);
        var assignment = await grantResponse.Content.ReadFromJsonAsync<RestaurantAdminAssignmentDto>(ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, manager.AccessToken);
        var managedRestaurantsResponse = await client.GetAsync("/api/v1/restaurant-admin/restaurants");
        var createSlotResponse = await client.PostAsJsonAsync(
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            new CreateRestaurantSlotRequest
            {
                StartsAtUtc = new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero),
                EndsAtUtc = new DateTimeOffset(2026, 5, 1, 22, 0, 0, TimeSpan.Zero),
                Capacity = 4,
                CutoffAtUtc = new DateTimeOffset(2026, 5, 1, 17, 0, 0, TimeSpan.Zero),
                MinThresholdForDiscount = 2,
                DiscountPercent = 25,
            },
            ApiTestHelpers.JsonOptions);
        var slot = await createSlotResponse.Content.ReadFromJsonAsync<RestaurantSlotDto>(ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, host.AccessToken);
        var createEventResponse = await client.PostAsJsonAsync(
            "/api/v1/events",
            new CreateEventRequest
            {
                EventType = EventType.Open,
                EventStartAtUtc = new DateTimeOffset(2026, 5, 1, 19, 0, 0, TimeSpan.Zero),
                Capacity = 4,
                Title = "Slot night",
                SelectedRestaurantId = restaurantId,
            },
            ApiTestHelpers.JsonOptions);
        var createdEvent = await createEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        var reserveResponse = await client.PostAsJsonAsync(
            $"/api/v1/events/{createdEvent!.EventId}/slot-reservations",
            new ReserveEventSlotRequest { SlotId = slot!.SlotId },
            ApiTestHelpers.JsonOptions);
        var reservation = await reserveResponse.Content.ReadFromJsonAsync<EventSlotReservationDto>(ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, guest.AccessToken);
        var joinResponse = await client.PostAsync($"/api/v1/events/{createdEvent!.EventId}/participants", content: null);

        ApiTestHelpers.SetBearer(client, host.AccessToken);
        var eventDetailResponse = await client.GetAsync($"/api/v1/events/{createdEvent.EventId}");
        var eventDetail = await eventDetailResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        var browseResponse = await client.GetAsync("/api/v1/events?page=1&pageSize=100");
        var browse = await browseResponse.Content.ReadFromJsonAsync<ListResponse<EventSummaryDto>>(ApiTestHelpers.JsonOptions);
        var summary = browse!.Items.Single(item => item.EventId == createdEvent.EventId);

        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        Assert.Equal(restaurantId, assignment!.RestaurantId);
        Assert.Equal(HttpStatusCode.OK, managedRestaurantsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createSlotResponse.StatusCode);
        Assert.Equal(25, slot!.DiscountPercent);
        Assert.Equal(HttpStatusCode.OK, reserveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        Assert.Equal(slot.SlotId, reservation!.SlotId);
        Assert.NotNull(eventDetail!.SlotReservation);
        Assert.Equal(restaurantId, eventDetail.SelectedRestaurantId);
        Assert.Null(eventDetail.CuisineTarget);
        Assert.NotNull(eventDetail.DiscountActivation);
        Assert.True(eventDetail.DiscountActivation!.IsActive);
        Assert.Equal(25, eventDetail.DiscountActivation.DiscountPercent);
        Assert.True(summary.HasActiveSlotReservation);
        Assert.True(summary.IsDiscountActive);
        Assert.Equal(25, summary.DiscountPercent);
    }

    [Fact]
    public async Task ReserveSlot_WhenSlotAlreadyReserved_ReturnsConflict()
    {
        using var enabledFactory = CreateEnabledFactory();
        enabledFactory.ResetState();
        using var client = enabledFactory.CreateClient();
        var restaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var admin = await ApiTestHelpers.RegisterAsync(client, username: "admin", email: "admin@example.com");
        var manager = await ApiTestHelpers.RegisterAsync(client, username: "manager", email: "manager@example.com");
        var firstHost = await ApiTestHelpers.RegisterAsync(client, username: "host1", email: "host1@example.com");
        var secondHost = await ApiTestHelpers.RegisterAsync(client, username: "host2", email: "host2@example.com");
        await ApiTestHelpers.PromoteRolesAsync(enabledFactory.Services, admin.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });

        ApiTestHelpers.SetBearer(client, admin.AccessToken);
        await client.PostAsJsonAsync(
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            new CreateRestaurantAdminAssignmentRequest { Username = "manager" },
            ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, manager.AccessToken);
        var slotResponse = await client.PostAsJsonAsync(
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            new CreateRestaurantSlotRequest
            {
                StartsAtUtc = new DateTimeOffset(2026, 5, 2, 18, 0, 0, TimeSpan.Zero),
                EndsAtUtc = new DateTimeOffset(2026, 5, 2, 22, 0, 0, TimeSpan.Zero),
                Capacity = 4,
                CutoffAtUtc = new DateTimeOffset(2026, 5, 2, 17, 0, 0, TimeSpan.Zero),
            },
            ApiTestHelpers.JsonOptions);
        var slot = await slotResponse.Content.ReadFromJsonAsync<RestaurantSlotDto>(ApiTestHelpers.JsonOptions);

        var firstEvent = await CreateEventAsync(client, firstHost, restaurantId, new DateTimeOffset(2026, 5, 2, 19, 0, 0, TimeSpan.Zero));
        var secondEvent = await CreateEventAsync(client, secondHost, restaurantId, new DateTimeOffset(2026, 5, 2, 19, 30, 0, TimeSpan.Zero));

        ApiTestHelpers.SetBearer(client, firstHost.AccessToken);
        await client.PostAsJsonAsync(
            $"/api/v1/events/{firstEvent.EventId}/slot-reservations",
            new ReserveEventSlotRequest { SlotId = slot!.SlotId },
            ApiTestHelpers.JsonOptions);

        ApiTestHelpers.SetBearer(client, secondHost.AccessToken);
        var conflict = await client.PostAsJsonAsync(
            $"/api/v1/events/{secondEvent.EventId}/slot-reservations",
            new ReserveEventSlotRequest { SlotId = slot.SlotId },
            ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Contains("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
    }

    private static TasteBudzApiFactory CreateEnabledFactory() =>
        new TasteBudzApiFactory().WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["FeatureFlags:RestaurantsOperationsEnabled"] = "true",
            ["FeatureFlags:RestaurantsSlotsEnabled"] = "true",
            ["FeatureFlags:RestaurantsDiscountsEnabled"] = "true",
        });

    private static TasteBudzApiFactory CreateDisabledFactory() =>
        new TasteBudzApiFactory().WithConfigurationOverrides(new Dictionary<string, string?>
        {
            ["FeatureFlags:RestaurantsOperationsEnabled"] = "false",
            ["FeatureFlags:RestaurantsSlotsEnabled"] = "false",
            ["FeatureFlags:RestaurantsDiscountsEnabled"] = "false",
        });

    private static async Task<EventDetailDto> CreateEventAsync(
        HttpClient client,
        SessionDto host,
        Guid restaurantId,
        DateTimeOffset eventStartAtUtc)
    {
        ApiTestHelpers.SetBearer(client, host.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/api/v1/events",
            new CreateEventRequest
            {
                EventType = EventType.Open,
                EventStartAtUtc = eventStartAtUtc,
                Capacity = 4,
                Title = $"Slot event {eventStartAtUtc:HHmm}",
                SelectedRestaurantId = restaurantId,
            },
            ApiTestHelpers.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions))!;
    }
}
