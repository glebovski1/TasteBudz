// HTTP endpoints for the seeded restaurant catalog and recommendation helpers.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/restaurants")]
/// <summary>
/// Exposes restaurant search, detail, and suggestion operations.
/// </summary>
public sealed class RestaurantsController(
    RestaurantSearchService restaurantSearchService,
    RestaurantRecommendationService restaurantRecommendationService) : ControllerBase
{
    [HttpGet]
    public Task<ListResponse<RestaurantDto>> Browse([FromQuery] BrowseRestaurantsQuery query, CancellationToken cancellationToken) =>
        restaurantSearchService.BrowseAsync(query, cancellationToken);

    [HttpGet("{restaurantId:guid}")]
    public Task<RestaurantDto> Get(Guid restaurantId, CancellationToken cancellationToken) =>
        restaurantSearchService.GetAsync(restaurantId, cancellationToken);

    [HttpGet("suggestions")]
    public Task<IReadOnlyCollection<RestaurantDto>> GetSuggestions([FromQuery] RestaurantSuggestionsQuery query, CancellationToken cancellationToken) =>
        restaurantRecommendationService.GetSuggestionsAsync(query, cancellationToken);
}