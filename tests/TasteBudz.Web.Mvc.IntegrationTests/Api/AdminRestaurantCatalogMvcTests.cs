using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AdminRestaurantCatalogMvcTests
{
    [Fact]
    public async Task RestaurantCatalogPage_RendersPagedRowsAndSingleEditForm()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var restaurantId = Guid.NewGuid();
        var secondRestaurantId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/restaurants/search?status=All&source=All&page=1&pageSize=25",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new TasteBudz.Backend.Contracts.ListResponse<AdminRestaurantCatalogItemDto>(
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
                    new AdminRestaurantCatalogItemDto(
                        secondRestaurantId,
                        "Taco Corner",
                        "456 Oak Ave",
                        "Cincinnati",
                        "OH",
                        "45202",
                        PriceTier.One,
                        new[] { "Mexican" },
                        39.10,
                        -84.50,
                        null,
                        false),
                },
                1001)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantAdminAssignmentDto>()));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/restaurants/{secondRestaurantId}/admin-assignments",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantAdminAssignmentDto>()));

        using var response = await client.GetAsync($"/Admin/Restaurants?editRestaurantId={restaurantId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("OpenStreetMap Import Preview", html);
        Assert.Contains("Create Restaurant", html);
        Assert.Contains("Ramen House", html);
        Assert.Contains("Taco Corner", html);
        Assert.Contains("123 Elm St", html);
        Assert.Contains("Cuisine Tags", html);
        Assert.Contains("Showing page 1 of 41 (1001 total).", html);
        Assert.Contains($"Edit Ramen House", html);
        Assert.DoesNotContain("Edit Taco Corner</h2>", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task RestaurantCatalogPage_DisablesBoundaryPaginationConsistently()
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
            "/api/v1/admin/restaurants/search?status=All&source=All&page=1&pageSize=25",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new TasteBudz.Backend.Contracts.ListResponse<AdminRestaurantCatalogItemDto>(
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
                            new[] { "Japanese" },
                            39.14,
                            -84.51,
                            null,
                            false),
                    },
                    1)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<RestaurantAdminAssignmentDto>()));

        using var response = await client.GetAsync("/Admin/Restaurants");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("aria-disabled=\"true\"", html);
        Assert.Contains("tabindex=\"-1\"", html);
        Assert.Contains("is-disabled", html);
        Assert.DoesNotContain("button--secondary disabled", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task RestaurantCatalogPreview_RendersImportCandidatesAndDuplicateWarnings()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var token = await LoginAndGetRestaurantPageTokenAsync(client, factory);

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/restaurants/import/preview?preset=cincinnati&zipCode=45202&radiusMiles=25",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new RestaurantImportPreviewDto(
                    new RestaurantImportGeographyDto("Cincinnati area within 25 miles of 45202", 38.9, -84.8, 39.3, -84.2, 39.1067, -84.512, 25),
                    new[]
                    {
                        new RestaurantImportCandidateDto(
                            "osm:node:1001",
                            "Preview Pho",
                            "300 Main St",
                            "Cincinnati",
                            "OH",
                            "45202",
                            "pho",
                            new[] { "Vietnamese" },
                            39.108,
                            -84.511,
                            false,
                            null,
                            null,
                            null),
                        new RestaurantImportCandidateDto(
                            "osm:node:777",
                            "Maki Social",
                            null,
                            "Cincinnati",
                            "OH",
                            "45220",
                            "sushi",
                            new[] { "Japanese" },
                            39.1276,
                            -84.5201,
                            true,
                            "Same name within 0.1 miles.",
                            Guid.NewGuid(),
                            "Maki Social"),
                    },
                    2,
                    1,
                    1)));
        EnqueueRestaurantCatalogShell(factory);

        using var response = await client.PostAsync(
            "/Admin/PreviewRestaurantImport",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Preset"] = "cincinnati",
                ["ZipCode"] = "45202",
                ["RadiusMiles"] = "25",
            }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("2 candidates", html);
        Assert.Contains("Preview Pho", html);
        Assert.Contains("name=\"SelectedExternalPlaceIds\" value=\"osm:node:1001\" checked", html);
        Assert.Contains("Duplicate", html);
        Assert.Contains("Same name within 0.1 miles.", html);
        Assert.Contains("Matched Maki Social.", html);
        factory.BackendHandler.AssertDrained();
    }

    private static async Task<string> LoginAndGetRestaurantPageTokenAsync(HttpClient client, TasteBudzMvcFactory factory)
    {
        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();
        EnqueueRestaurantCatalogShell(factory);
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Admin/Restaurants");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();
        return token;
    }

    private static void EnqueueRestaurantCatalogShell(TasteBudzMvcFactory factory)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants/search?status=All&source=All&page=1&pageSize=25",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new TasteBudz.Backend.Contracts.ListResponse<AdminRestaurantCatalogItemDto>(Array.Empty<AdminRestaurantCatalogItemDto>(), 0)));
    }
}
