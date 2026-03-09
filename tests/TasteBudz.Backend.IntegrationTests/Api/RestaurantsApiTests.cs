// Integration tests for restaurant browse and suggestion endpoints.
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies that the seeded catalog is reachable through the documented HTTP surface.
/// </summary>
public sealed class RestaurantsApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    /// <summary>
    /// Basic browse and suggestion filters should work against the deterministic seeded catalog.
    /// </summary>
    [Fact]
    public async Task BrowseAndSuggestions_UseSeededCatalogFilters()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "foodie", email: "foodie@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var browseResponse = await client.GetAsync("/api/v1/restaurants?cuisine=Sushi&zipCode=45220&radiusMiles=5&pageSize=10");
        var suggestionResponse = await client.GetAsync("/api/v1/restaurants/suggestions?zipCode=45220&cuisineTags=Sushi");

        var browse = await browseResponse.Content.ReadFromJsonAsync<ListResponse<RestaurantDto>>(ApiTestHelpers.JsonOptions);
        var suggestions = await suggestionResponse.Content.ReadFromJsonAsync<RestaurantDto[]>(ApiTestHelpers.JsonOptions);
        var restaurant = Assert.Single(browse!.Items);

        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Equal("Maki Social", restaurant.Name);
        Assert.Equal(HttpStatusCode.OK, suggestionResponse.StatusCode);
        Assert.NotEmpty(suggestions!);
        Assert.Contains(suggestions!, item => item.Name == "Maki Social");
    }

    /// <summary>
    /// Unknown group ids should fail clearly rather than silently degrading into generic suggestions.
    /// </summary>
    [Fact]
    public async Task Suggestions_WithUnknownGroupId_ReturnNotFoundProblemDetails()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "foodie", email: "foodie@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.GetAsync($"/api/v1/restaurants/suggestions?groupId={Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, problem!.Status);
    }

    /// <summary>
    /// Group context should provide distance input when the caller does not specify a ZIP directly.
    /// </summary>
    [Fact]
    public async Task Suggestions_WithGroupId_UseGroupOwnerZipContext()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "owner", email: "owner@example.com", zipCode: "41011");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var groupId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (var scope = factory.Services.CreateScope())
        {
            // Seed the group directly through the repository so the HTTP test exercises the real suggestion endpoint only.
            var groupRepository = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
            await groupRepository.SaveAsync(new Group(groupId, session.CurrentUser.UserId, "Dinner Club", null, GroupVisibility.Public, GroupLifecycleState.Active, now, now));
            await groupRepository.SaveMemberAsync(new GroupMember(groupId, session.CurrentUser.UserId, GroupMemberState.Active, now, now));
        }

        var response = await client.GetAsync($"/api/v1/restaurants/suggestions?groupId={groupId}&radiusMiles=1");
        var suggestions = await response.Content.ReadFromJsonAsync<RestaurantDto[]>(ApiTestHelpers.JsonOptions);
        var restaurant = Assert.Single(suggestions!);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Riverfront Grill", restaurant.Name);
    }

    [Fact]
    public async Task RestaurantDetail_WithUnknownIdReturnsProblemDetails()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "foodie", email: "foodie@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var response = await client.GetAsync($"/api/v1/restaurants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
