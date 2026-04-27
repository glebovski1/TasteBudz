using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over restaurant browse, admin catalog, restaurant-admin operations, and suggestion endpoints.
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

    public async Task<IReadOnlyCollection<RestaurantDto>> BrowseAllAsync(
        BrowseRestaurantsQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        const int maxPageSize = 2000;
        var baseQuery = query ?? new BrowseRestaurantsQuery();
        var items = new List<RestaurantDto>();
        var page = Math.Max(baseQuery.Page, 1);
        var totalCount = int.MaxValue;

        while (items.Count < totalCount)
        {
            var response = await BrowseAsync(
                CloneBrowseQuery(baseQuery, page, maxPageSize),
                cancellationToken);

            items.AddRange(response.Items);
            totalCount = response.TotalCount;

            if (response.Items.Count == 0)
            {
                break;
            }

            page++;
        }

        return items;
    }

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

    /// <summary>
    /// Triggers the Overpass restaurant import on the backend. Admin only.
    /// Returns the number of new restaurants inserted.
    /// </summary>
    public Task<ImportResultDto> ImportFromOverpassAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<ImportResultDto>(
            "/api/v1/restaurants/import",
            cancellationToken: cancellationToken);

    public Task<RestaurantImportPreviewDto> PreviewImportFromOverpassAsync(
        RestaurantImportPreviewQuery query,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<RestaurantImportPreviewDto>(
            BuildImportPreviewPath(query),
            cancellationToken);

    public Task<ImportResultDto> CommitImportFromOverpassAsync(
        CommitRestaurantImportRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CommitRestaurantImportRequest, ImportResultDto>(
            "/api/v1/restaurants/import/commit",
            request,
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<AdminRestaurantCatalogItemDto>> ListAdminRestaurantsAsync(CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<AdminRestaurantCatalogItemDto>>(
            "/api/v1/admin/restaurants",
            cancellationToken);

    public Task<ListResponse<AdminRestaurantCatalogItemDto>> SearchAdminRestaurantsAsync(
        AdminRestaurantSearchQuery query,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<AdminRestaurantCatalogItemDto>>(
            BuildAdminRestaurantSearchPath(query),
            cancellationToken);

    public Task<AdminRestaurantCatalogItemDto> CreateAdminRestaurantAsync(
        SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<SaveRestaurantCatalogRequest, AdminRestaurantCatalogItemDto>(
            "/api/v1/admin/restaurants",
            request,
            cancellationToken: cancellationToken);

    public Task<AdminRestaurantCatalogItemDto> UpdateAdminRestaurantAsync(
        Guid restaurantId,
        SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<SaveRestaurantCatalogRequest, AdminRestaurantCatalogItemDto>(
            $"/api/v1/admin/restaurants/{restaurantId}",
            request,
            cancellationToken);

    public Task ArchiveAdminRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/admin/restaurants/{restaurantId}/archive",
            cancellationToken: cancellationToken);

    public Task RestoreAdminRestaurantAsync(Guid restaurantId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/admin/restaurants/{restaurantId}/restore",
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

    public Task<RestaurantSlotDto> UpdateManagedSlotAsync(
        Guid slotId,
        UpdateRestaurantSlotRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateRestaurantSlotRequest, RestaurantSlotDto>(
            $"/api/v1/restaurant-admin/slots/{slotId}",
            request,
            cancellationToken);

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
            builder.Add("q", query.Q);
        if (!string.IsNullOrWhiteSpace(query.Cuisine))
            builder.Add("cuisine", query.Cuisine);
        if (query.PriceTier.HasValue)
            builder.Add("priceTier", query.PriceTier.Value.ToString());
        if (query.HasDiscountSlots)
            builder.Add("hasDiscountSlots", "true");
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

    private static string BuildAdminRestaurantSearchPath(AdminRestaurantSearchQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
            builder.Add("q", query.Q);
        if (query.Status.HasValue)
            builder.Add("status", query.Status.Value.ToString());
        if (query.Source.HasValue)
            builder.Add("source", query.Source.Value.ToString());

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/admin/restaurants/search{builder.ToQueryString()}";
    }

    private static string BuildImportPreviewPath(RestaurantImportPreviewQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Preset))
            builder.Add("preset", query.Preset);
        if (!string.IsNullOrWhiteSpace(query.ZipCode))
            builder.Add("zipCode", query.ZipCode);
        if (query.RadiusMiles.HasValue)
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));
        if (query.South.HasValue)
            builder.Add("south", query.South.Value.ToString(CultureInfo.InvariantCulture));
        if (query.West.HasValue)
            builder.Add("west", query.West.Value.ToString(CultureInfo.InvariantCulture));
        if (query.North.HasValue)
            builder.Add("north", query.North.Value.ToString(CultureInfo.InvariantCulture));
        if (query.East.HasValue)
            builder.Add("east", query.East.Value.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/restaurants/import/preview{builder.ToQueryString()}";
    }

    private static BrowseRestaurantsQuery CloneBrowseQuery(BrowseRestaurantsQuery query, int page, int pageSize) =>
        new()
        {
            Q = query.Q,
            Cuisine = query.Cuisine,
            PriceTier = query.PriceTier,
            HasDiscountSlots = query.HasDiscountSlots,
            ZipCode = query.ZipCode,
            RadiusMiles = query.RadiusMiles,
            Page = page,
            PageSize = pageSize,
        };
}
