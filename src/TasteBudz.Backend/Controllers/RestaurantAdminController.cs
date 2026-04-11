using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Controllers;

[Authorize(Policy = "RestaurantAdmin")]
[ApiController]
[Route("api/v1/restaurant-admin")]
public sealed class RestaurantAdminController(
    ManagedRestaurantService managedRestaurantService,
    RestaurantSlotService restaurantSlotService,
    IFeatureFlagService featureFlagService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("restaurants")]
    public Task<IReadOnlyCollection<RestaurantDto>> ListManagedRestaurants(CancellationToken cancellationToken)
    {
        EnsureOperationsEnabled();
        return managedRestaurantService.ListManagedAsync(currentUserAccessor.GetRequiredCurrentUser(), cancellationToken);
    }

    [HttpPatch("restaurants/{restaurantId:guid}")]
    public Task<RestaurantDto> UpdateManagedRestaurant(
        Guid restaurantId,
        [FromBody] UpdateManagedRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOperationsEnabled();
        return managedRestaurantService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, request, cancellationToken);
    }

    [HttpGet("restaurants/{restaurantId:guid}/slots")]
    public Task<IReadOnlyCollection<RestaurantSlotDto>> ListSlots(Guid restaurantId, CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        return restaurantSlotService.ListManagedAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, cancellationToken);
    }

    [HttpPost("restaurants/{restaurantId:guid}/slots")]
    public Task<RestaurantSlotDto> CreateSlot(
        Guid restaurantId,
        [FromBody] CreateRestaurantSlotRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        return restaurantSlotService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, request, cancellationToken);
    }

    [HttpPatch("slots/{slotId:guid}")]
    public Task<RestaurantSlotDto> UpdateSlot(
        Guid slotId,
        [FromBody] UpdateRestaurantSlotRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        return restaurantSlotService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser(), slotId, request, cancellationToken);
    }

    [HttpPost("slots/{slotId:guid}/cancellation")]
    public async Task<IActionResult> CancelSlot(
        Guid slotId,
        [FromBody] CancelRestaurantSlotRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSlotsEnabled();
        await restaurantSlotService.CancelAsync(currentUserAccessor.GetRequiredCurrentUser(), slotId, request, cancellationToken);
        return NoContent();
    }

    private void EnsureOperationsEnabled()
    {
        if (!featureFlagService.IsRestaurantsOperationsEnabled())
        {
            throw ApiException.NotFound("Restaurant operations are not enabled.");
        }
    }

    private void EnsureSlotsEnabled()
    {
        EnsureOperationsEnabled();

        if (!featureFlagService.IsRestaurantsSlotsEnabled())
        {
            throw ApiException.NotFound("Restaurant slots are not enabled.");
        }
    }
}
