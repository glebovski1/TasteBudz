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
            "/api/v1/restaurants?q=ramen&cuisine=Japanese&priceTier=Three&hasDiscountSlots=true&zipCode=45220&radiusMiles=5.5&page=2&pageSize=15",
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
            HasDiscountSlots = true,
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

    [Fact]
    public async Task BrowseAllAsync_PaginatesUntilAllRestaurantsAreLoaded()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new RestaurantApiService(client));

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=1&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    Enumerable.Range(1, 2000)
                        .Select(index => new RestaurantDto(Guid.NewGuid(), $"Restaurant {index}", "Cincinnati", "OH", "45220", PriceTier.Two, new[] { "Thai" }, null, null, null, null))
                        .ToArray(),
                    2001)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants?page=2&pageSize=2000",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<RestaurantDto>(
                    new[]
                    {
                        new RestaurantDto(Guid.NewGuid(), "Restaurant 2001", "Cincinnati", "OH", "45220", PriceTier.Two, new[] { "Thai" }, null, null, null, null),
                    },
                    2001)));

        var restaurants = await service.BrowseAllAsync();

        Assert.Equal(2001, restaurants.Count);
        Assert.Contains(restaurants, restaurant => restaurant.Name == "Restaurant 2001");
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ImportPreviewAndCommit_SendExpectedRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new RestaurantApiService(client));

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants/import/preview?preset=cincinnati&zipCode=45202&radiusMiles=10&south=39&west=-85&north=40&east=-84",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantImportPreviewDto(
                    new RestaurantImportGeographyDto("Manual bounds", 39, -85, 40, -84, null, null, null),
                    Array.Empty<RestaurantImportCandidateDto>(),
                    0,
                    0,
                    0)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/restaurants/import/commit",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ImportResultDto(1, "Import complete. 1 new restaurants added. 0 skipped.", 0)));

        var preview = await service.PreviewImportFromOverpassAsync(new RestaurantImportPreviewQuery
        {
            Preset = "cincinnati",
            ZipCode = "45202",
            RadiusMiles = 10,
            South = 39,
            West = -85,
            North = 40,
            East = -84,
        });
        var commit = await service.CommitImportFromOverpassAsync(new CommitRestaurantImportRequest
        {
            Preset = "cincinnati",
            ZipCode = "45202",
            RadiusMiles = 10,
            SelectedExternalPlaceIds = new[] { "osm:node:1001" },
        });

        Assert.Equal("Manual bounds", preview.Geography.Label);
        Assert.Equal(1, commit.Inserted);
        Assert.Contains(
            "\"selectedExternalPlaceIds\":[\"osm:node:1001\"]",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/restaurants/import/commit").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ImportFromOverpassAsync_SendsExpectedRoute()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new RestaurantApiService(client));

        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/restaurants/import",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ImportResultDto(12, "Import complete. 12 new restaurants added.")));

        var result = await service.ImportFromOverpassAsync();

        Assert.Equal(12, result.Inserted);
        Assert.Equal("Import complete. 12 new restaurants added.", result.Message);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task AdminCatalogAndRestaurantOperations_SendExpectedRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new RestaurantApiService(client));
        var restaurantId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var adminRestaurant = new AdminRestaurantCatalogItemDto(
            restaurantId,
            "Ramen House",
            "123 Elm St",
            "Cincinnati",
            "OH",
            "45220",
            PriceTier.Three,
            new[] { "Japanese" },
            39.14,
            -84.51,
            "osm:node:123",
            false);

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, new[] { adminRestaurant }));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants/search?q=ramen&status=Active&source=OpenStreetMap&page=2&pageSize=25",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<AdminRestaurantCatalogItemDto>(new[] { adminRestaurant }, 1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, adminRestaurant));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/admin/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, adminRestaurant with { Name = "Updated Ramen House" }));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/restaurants/{restaurantId}/archive",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/restaurants/{restaurantId}/restore",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantSlotDto>()));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantAdminAssignmentDto>()));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantAdminAssignmentDto(restaurantId, userId, "manager", DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments/{userId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurant-admin/restaurants",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantDto>()));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantDto(restaurantId, "Updated", "Cincinnati", "OH", "45220", PriceTier.Two, Array.Empty<string>(), null, null, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantSlotDto>()));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantSlotDto(slotId, restaurantId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2), 4, DateTimeOffset.UtcNow, null, null, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/restaurant-admin/slots/{slotId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantSlotDto(slotId, restaurantId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(3), 6, DateTimeOffset.UtcNow, 4, 20, RestaurantSlotStatus.Open, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/restaurant-admin/slots/{slotId}/cancellation",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        await service.ListAdminRestaurantsAsync();
        await service.SearchAdminRestaurantsAsync(new AdminRestaurantSearchQuery
        {
            Q = "ramen",
            Status = AdminRestaurantCatalogStatus.Active,
            Source = AdminRestaurantCatalogSource.OpenStreetMap,
            Page = 2,
            PageSize = 25,
        });
        await service.CreateAdminRestaurantAsync(new SaveRestaurantCatalogRequest
        {
            Name = "Ramen House",
            StreetAddress = "123 Elm St",
            City = "Cincinnati",
            State = "OH",
            ZipCode = "45220",
            PriceTier = PriceTier.Three,
            CuisineTags = new[] { "Japanese" },
        });
        await service.UpdateAdminRestaurantAsync(restaurantId, new SaveRestaurantCatalogRequest
        {
            Name = "Updated Ramen House",
            StreetAddress = "456 Oak Ave",
            City = "Cincinnati",
            State = "OH",
            ZipCode = "45220",
            PriceTier = PriceTier.Three,
            CuisineTags = new[] { "Japanese", "Sushi" },
        });
        await service.ArchiveAdminRestaurantAsync(restaurantId);
        await service.RestoreAdminRestaurantAsync(restaurantId);
        await service.ListReservableSlotsAsync(restaurantId);
        await service.ListAdminAssignmentsAsync(restaurantId);
        await service.GrantAdminAssignmentAsync(restaurantId, new CreateRestaurantAdminAssignmentRequest { Username = "manager" });
        await service.RevokeAdminAssignmentAsync(restaurantId, userId);
        await service.ListManagedRestaurantsAsync();
        await service.UpdateManagedRestaurantAsync(restaurantId, new UpdateManagedRestaurantRequest { Name = "Updated" });
        await service.ListManagedSlotsAsync(restaurantId);
        await service.CreateManagedSlotAsync(restaurantId, new CreateRestaurantSlotRequest
        {
            StartsAtUtc = DateTimeOffset.UtcNow,
            EndsAtUtc = DateTimeOffset.UtcNow.AddHours(2),
            Capacity = 4,
            CutoffAtUtc = DateTimeOffset.UtcNow,
        });
        await service.UpdateManagedSlotAsync(slotId, new UpdateRestaurantSlotRequest
        {
            Capacity = 6,
            MinThresholdForDiscount = 4,
            DiscountPercent = 20,
        });
        await service.CancelManagedSlotAsync(slotId, new CancelRestaurantSlotRequest { Reason = "Closed." });

        Assert.Contains(
            "\"username\":\"manager\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments" && request.Method == HttpMethod.Post).Body);
        Assert.Contains(
            "\"streetAddress\":\"123 Elm St\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/admin/restaurants" && request.Method == HttpMethod.Post).Body);
        Assert.Contains(
            "\"cuisineTags\":[\"Japanese\",\"Sushi\"]",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/admin/restaurants/{restaurantId}" && request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"name\":\"Updated\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/restaurants/{restaurantId}" && request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"capacity\":6",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}" && request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"discountPercent\":20",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/restaurant-admin/slots/{slotId}" && request.Method == HttpMethod.Patch).Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
