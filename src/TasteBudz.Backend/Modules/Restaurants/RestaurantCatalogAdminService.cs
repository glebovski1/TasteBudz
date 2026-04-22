using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Admin-only maintenance operations for the internal restaurant catalog.
/// </summary>
public sealed class RestaurantCatalogAdminService(
    IRestaurantRepository restaurantRepository,
    IRestaurantGeocodingService restaurantGeocodingService)
{
    public async Task<IReadOnlyCollection<AdminRestaurantCatalogItemDto>> ListAsync(
        CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);

        var restaurants = await restaurantRepository.ListAsync(includeArchived: true, cancellationToken: cancellationToken);
        return restaurants
            .OrderBy(restaurant => restaurant.IsArchived)
            .ThenBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToAdminDto)
            .ToArray();
    }

    public async Task<AdminRestaurantCatalogItemDto> CreateAsync(
        CurrentUser currentUser,
        SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        var normalized = await NormalizeAsync(request, currentRestaurant: null, cancellationToken);
        var restaurant = new Restaurant(
            Guid.NewGuid(),
            normalized.Name,
            normalized.City,
            normalized.State,
            normalized.ZipCode,
            normalized.Latitude,
            normalized.Longitude,
            normalized.PriceTier,
            normalized.CuisineTags,
            normalized.ExternalPlaceId,
            normalized.StreetAddress,
            false);

        await restaurantRepository.SaveAsync(restaurant, cancellationToken);
        return ToAdminDto(restaurant);
    }

    public async Task<AdminRestaurantCatalogItemDto> UpdateAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        var restaurant = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");
        var normalized = await NormalizeAsync(request, restaurant, cancellationToken);
        var updated = restaurant with
        {
            Name = normalized.Name,
            StreetAddress = normalized.StreetAddress,
            City = normalized.City,
            State = normalized.State,
            ZipCode = normalized.ZipCode,
            Latitude = normalized.Latitude,
            Longitude = normalized.Longitude,
            PriceTier = normalized.PriceTier,
            CuisineTags = normalized.CuisineTags,
            ExternalPlaceId = normalized.ExternalPlaceId,
        };

        await restaurantRepository.SaveAsync(updated, cancellationToken);
        return ToAdminDto(updated);
    }

    public async Task ArchiveAsync(CurrentUser currentUser, Guid restaurantId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        var restaurant = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        if (restaurant.IsArchived)
        {
            return;
        }

        await restaurantRepository.SaveAsync(restaurant with { IsArchived = true }, cancellationToken);
    }

    public async Task RestoreAsync(CurrentUser currentUser, Guid restaurantId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUser);
        var restaurant = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        if (!restaurant.IsArchived)
        {
            return;
        }

        await restaurantRepository.SaveAsync(restaurant with { IsArchived = false }, cancellationToken);
    }

    private async Task<NormalizedRestaurantCatalogRequest> NormalizeAsync(
        SaveRestaurantCatalogRequest request,
        Restaurant? currentRestaurant,
        CancellationToken cancellationToken)
    {
        var name = NormalizeRequired(request.Name, "name");
        var streetAddress = NormalizeOptional(request.StreetAddress) ?? currentRestaurant?.StreetAddress;
        var city = NormalizeRequired(request.City, "city");
        var state = NormalizeRequired(request.State, "state").ToUpperInvariant();
        var zipCode = NormalizeRequired(request.ZipCode, "zipCode");
        var priceTier = request.PriceTier ?? currentRestaurant?.PriceTier
            ?? throw ApiException.BadRequest("priceTier is required.");
        var cuisineTags = NormalizeCuisineTags(request.CuisineTags);
        var geocodeResult = await restaurantGeocodingService.GeocodeAsync(name, streetAddress, city, state, zipCode, cancellationToken);

        if (geocodeResult is null)
        {
            throw ApiException.BadRequest("The restaurant address could not be geocoded. Check the address and ZIP code.");
        }

        return new NormalizedRestaurantCatalogRequest(
            name,
            streetAddress,
            city,
            state,
            zipCode,
            priceTier,
            cuisineTags,
            geocodeResult.Latitude,
            geocodeResult.Longitude,
            geocodeResult.ExternalPlaceId ?? currentRestaurant?.ExternalPlaceId);
    }

    private static IReadOnlyCollection<string> NormalizeCuisineTags(IReadOnlyCollection<string> values)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            throw ApiException.BadRequest("At least one cuisine tag is required.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw ApiException.BadRequest($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static void EnsureAdmin(CurrentUser currentUser)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw ApiException.Forbidden("Admin access is required.");
        }
    }

    private static AdminRestaurantCatalogItemDto ToAdminDto(Restaurant restaurant) =>
        new(
            restaurant.Id,
            restaurant.Name,
            restaurant.StreetAddress,
            restaurant.City,
            restaurant.State,
            restaurant.ZipCode,
            restaurant.PriceTier,
            restaurant.CuisineTags,
            restaurant.Latitude,
            restaurant.Longitude,
            restaurant.ExternalPlaceId,
            restaurant.IsArchived);

    private sealed record NormalizedRestaurantCatalogRequest(
        string Name,
        string? StreetAddress,
        string City,
        string State,
        string ZipCode,
        PriceTier PriceTier,
        IReadOnlyCollection<string> CuisineTags,
        double Latitude,
        double Longitude,
        string? ExternalPlaceId);
}
