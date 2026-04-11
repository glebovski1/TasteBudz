using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Handles restaurant-admin access to assigned restaurant profile data.
/// </summary>
public sealed class ManagedRestaurantService(
    IRestaurantRepository restaurantRepository,
    IRestaurantOperationsRepository restaurantOperationsRepository)
{
    public async Task<IReadOnlyCollection<RestaurantDto>> ListManagedAsync(CurrentUser currentUser, CancellationToken cancellationToken = default)
    {
        EnsureRestaurantAdminRole(currentUser);
        var assignments = await restaurantOperationsRepository.ListAssignmentsForUserAsync(currentUser.UserId, cancellationToken);
        var restaurants = new List<RestaurantDto>(assignments.Count);

        foreach (var assignment in assignments)
        {
            var restaurant = await restaurantRepository.GetAsync(assignment.RestaurantId, cancellationToken);

            if (restaurant is not null)
            {
                restaurants.Add(RestaurantSearchService.ToDto(restaurant, null));
            }
        }

        return restaurants.OrderBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<RestaurantDto> UpdateAsync(
        CurrentUser currentUser,
        Guid restaurantId,
        UpdateManagedRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageRestaurantAsync(currentUser, restaurantId, cancellationToken);
        var restaurant = await restaurantRepository.GetAsync(restaurantId, cancellationToken)
            ?? throw ApiException.NotFound("The requested restaurant could not be found.");

        var updated = restaurant with
        {
            Name = NormalizeRequiredPatch(request.Name, restaurant.Name, "name"),
            City = NormalizeRequiredPatch(request.City, restaurant.City, "city"),
            State = NormalizeRequiredPatch(request.State, restaurant.State, "state").ToUpperInvariant(),
            ZipCode = NormalizeRequiredPatch(request.ZipCode, restaurant.ZipCode, "zipCode"),
            PriceTier = request.PriceTier ?? restaurant.PriceTier,
            ExternalPlaceId = request.ExternalPlaceId is null ? restaurant.ExternalPlaceId : NormalizeOptional(request.ExternalPlaceId),
        };

        await restaurantOperationsRepository.SaveRestaurantAsync(updated, cancellationToken);
        return RestaurantSearchService.ToDto(updated, null);
    }

    public async Task EnsureCanManageRestaurantAsync(CurrentUser currentUser, Guid restaurantId, CancellationToken cancellationToken)
    {
        EnsureRestaurantAdminRole(currentUser);

        var assignment = await restaurantOperationsRepository.GetAssignmentAsync(restaurantId, currentUser.UserId, cancellationToken);

        if (assignment is null || assignment.RevokedAtUtc is not null)
        {
            throw ApiException.Forbidden("You are not assigned to manage this restaurant.");
        }
    }

    private static void EnsureRestaurantAdminRole(CurrentUser currentUser)
    {
        if (!currentUser.IsInRole(UserRole.RestaurantAdmin))
        {
            throw ApiException.Forbidden("Restaurant admin access is required.");
        }
    }

    private static string NormalizeRequiredPatch(string? candidate, string fallback, string fieldName)
    {
        if (candidate is null)
        {
            return fallback;
        }

        var normalized = candidate.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw ApiException.BadRequest($"{fieldName} cannot be blank.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
