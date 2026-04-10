using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over restaurant browse, detail, suggestion, and import endpoints.
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

    /// <summary>
    /// Triggers the Overpass restaurant import on the backend. Admin only.
    /// Returns the number of new restaurants inserted.
    /// </summary>
    public Task<ImportResultDto> ImportFromOverpassAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<ImportResultDto>(
            "/api/v1/restaurants/import",
            cancellationToken: cancellationToken);

    private static string BuildBrowsePath(BrowseRestaurantsQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
            builder.Add("q", query.Q);
        if (!string.IsNullOrWhiteSpace(query.Cuisine))
            builder.Add("cuisine", query.Cuisine);
        if (query.PriceTier.HasValue)
            builder.Add("priceTier", query.PriceTier.Value.ToString());
        if (!string.IsNullOrWhiteSpace(query.ZipCode))
            builder.Add("zipCode", query.ZipCode);
        if (query.RadiusMiles.HasValue)
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/restaurants{builder.ToQueryString()}";
    }

    private static string BuildSuggestionsPath(RestaurantSuggestionsQuery query)
    {
        var builder = new QueryBuilder();

        if (query.EventId.HasValue)
            builder.Add("eventId", query.EventId.Value.ToString());
        if (query.GroupId.HasValue)
            builder.Add("groupId", query.GroupId.Value.ToString());
        if (!string.IsNullOrWhiteSpace(query.ZipCode))
            builder.Add("zipCode", query.ZipCode);
        if (query.RadiusMiles.HasValue)
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));

        foreach (var cuisineTag in query.CuisineTags)
            builder.Add("cuisineTags", cuisineTag);

        return $"/api/v1/restaurants/suggestions{builder.ToQueryString()}";
    }
}

/// <summary>Response shape returned by POST /api/v1/restaurants/import.</summary>
public sealed record ImportResultDto(int Inserted, string Message);