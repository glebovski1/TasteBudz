// HTTP endpoints for the seeded restaurant catalog and recommendation helpers.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/restaurants")]
/// <summary>
/// Exposes restaurant search, detail, suggestion, and catalog import operations.
/// </summary>
public sealed class RestaurantsController(
    RestaurantSearchService restaurantSearchService,
    RestaurantRecommendationService restaurantRecommendationService,
    RestaurantSlotService restaurantSlotService,
    OverpassRestaurantImporter overpassImporter,
    IFeatureFlagService featureFlagService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<ListResponse<RestaurantDto>> Browse([FromQuery] BrowseRestaurantsQuery query, CancellationToken cancellationToken) =>
        restaurantSearchService.BrowseAsync(query, cancellationToken);

    [HttpGet("{restaurantId:guid}")]
    public Task<RestaurantDto> Get(Guid restaurantId, CancellationToken cancellationToken) =>
        restaurantSearchService.GetAsync(restaurantId, cancellationToken);

    [HttpGet("suggestions")]
    public Task<IReadOnlyCollection<RestaurantDto>> GetSuggestions([FromQuery] RestaurantSuggestionsQuery query, CancellationToken cancellationToken) =>
        restaurantRecommendationService.GetSuggestionsAsync(currentUserAccessor.GetRequiredCurrentUser(), query, cancellationToken);

    [HttpGet("{restaurantId:guid}/slots")]
    public Task<IReadOnlyCollection<RestaurantSlotDto>> ListReservableSlots(Guid restaurantId, CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        return restaurantSlotService.ListReservableAsync(restaurantId, cancellationToken);
    }

    /// <summary>
    /// Triggers a live import of restaurants from OpenStreetMap (Overpass API).
    /// Restricted to Admin role. Safe to call multiple times; skips existing entries.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        var inserted = await overpassImporter.ImportAsync(cancellationToken);
        return Ok(new ImportRestaurantsResultDto(inserted, $"Import complete. {inserted} new restaurants added."));
    }

    private void EnsureSlotsEnabled()
    {
        if (!featureFlagService.IsRestaurantsOperationsEnabled() ||
            !featureFlagService.IsRestaurantsSlotsEnabled())
        {
            throw ApiException.NotFound("Restaurant slots are not enabled.");
        }
    }
}
