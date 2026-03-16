using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over restaurant browse, detail, and suggestion endpoints.
/// </summary>
public sealed class RestaurantApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public RestaurantApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<ListResponse<RestaurantDto>> BrowseAsync(
        BrowseRestaurantsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<RestaurantDto>>(
            BuildBrowsePath(query ?? new BrowseRestaurantsQuery()),
            cancellationToken);

    public Task<RestaurantDto> GetAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<RestaurantDto>($"/api/v1/restaurants/{restaurantId}", cancellationToken);

    public Task<IReadOnlyCollection<RestaurantDto>> GetSuggestionsAsync(
        RestaurantSuggestionsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantDto>>(
            BuildSuggestionsPath(query ?? new RestaurantSuggestionsQuery()),
            cancellationToken);

    private static string BuildBrowsePath(BrowseRestaurantsQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            builder.Add("q", query.Q);
        }

        if (!string.IsNullOrWhiteSpace(query.Cuisine))
        {
            builder.Add("cuisine", query.Cuisine);
        }

        if (query.PriceTier.HasValue)
        {
            builder.Add("priceTier", query.PriceTier.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.ZipCode))
        {
            builder.Add("zipCode", query.ZipCode);
        }

        if (query.RadiusMiles.HasValue)
        {
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/restaurants{builder.ToQueryString()}";
    }

    private static string BuildSuggestionsPath(RestaurantSuggestionsQuery query)
    {
        var builder = new QueryBuilder();

        if (query.EventId.HasValue)
        {
            builder.Add("eventId", query.EventId.Value.ToString());
        }

        if (query.GroupId.HasValue)
        {
            builder.Add("groupId", query.GroupId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.ZipCode))
        {
            builder.Add("zipCode", query.ZipCode);
        }

        if (query.RadiusMiles.HasValue)
        {
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var cuisineTag in query.CuisineTags)
        {
            builder.Add("cuisineTags", cuisineTag);
        }

        return $"/api/v1/restaurants/suggestions{builder.ToQueryString()}";
    }
}
