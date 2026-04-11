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

    public Task<IReadOnlyCollection<RestaurantSlotDto>> ListReservableSlotsAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantSlotDto>>($"/api/v1/restaurants/{restaurantId}/slots", cancellationToken);

    public Task<IReadOnlyCollection<RestaurantDto>> GetSuggestionsAsync(
        RestaurantSuggestionsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantDto>>(
            BuildSuggestionsPath(query ?? new RestaurantSuggestionsQuery()),
            cancellationToken);

    public Task<ImportResultDto> ImportFromOverpassAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<ImportResultDto>(
            "/api/v1/restaurants/import",
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<RestaurantAdminAssignmentDto>> ListAdminAssignmentsAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantAdminAssignmentDto>>(
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            cancellationToken);

    public Task<RestaurantAdminAssignmentDto> GrantAdminAssignmentAsync(
        Guid restaurantId,
        CreateRestaurantAdminAssignmentRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateRestaurantAdminAssignmentRequest, RestaurantAdminAssignmentDto>(
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments",
            request,
            cancellationToken: cancellationToken);

    public Task RevokeAdminAssignmentAsync(Guid restaurantId, Guid userId, CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync(
            $"/api/v1/admin/restaurants/{restaurantId}/admin-assignments/{userId}",
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<RestaurantDto>> ListManagedRestaurantsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantDto>>("/api/v1/restaurant-admin/restaurants", cancellationToken);

    public Task<RestaurantDto> UpdateManagedRestaurantAsync(
        Guid restaurantId,
        UpdateManagedRestaurantRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateManagedRestaurantRequest, RestaurantDto>(
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}",
            request,
            cancellationToken);

    public Task<IReadOnlyCollection<RestaurantSlotDto>> ListManagedSlotsAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<RestaurantSlotDto>>(
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            cancellationToken);

    public Task<RestaurantSlotDto> CreateManagedSlotAsync(
        Guid restaurantId,
        CreateRestaurantSlotRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateRestaurantSlotRequest, RestaurantSlotDto>(
            $"/api/v1/restaurant-admin/restaurants/{restaurantId}/slots",
            request,
            cancellationToken: cancellationToken);

    public Task CancelManagedSlotAsync(
        Guid slotId,
        CancelRestaurantSlotRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/restaurant-admin/slots/{slotId}/cancellation",
            request,
            cancellationToken: cancellationToken);

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

public sealed record ImportResultDto(int Inserted, string Message);
