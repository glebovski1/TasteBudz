using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/restaurants/{restaurantId:guid}/admin-assignments")]
public sealed class AdminRestaurantAssignmentsController(
    RestaurantAdminAssignmentService assignmentService,
    IFeatureFlagService featureFlagService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<RestaurantAdminAssignmentDto>> List(Guid restaurantId, CancellationToken cancellationToken)
    {
        EnsureOperationsEnabled();
        return assignmentService.ListAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, cancellationToken);
    }

    [HttpPost]
    public Task<RestaurantAdminAssignmentDto> Grant(
        Guid restaurantId,
        [FromBody] CreateRestaurantAdminAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOperationsEnabled();
        return assignmentService.GrantAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, request, cancellationToken);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Revoke(Guid restaurantId, Guid userId, CancellationToken cancellationToken)
    {
        EnsureOperationsEnabled();
        await assignmentService.RevokeAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, userId, cancellationToken);
        return NoContent();
    }

    private void EnsureOperationsEnabled()
    {
        if (!featureFlagService.IsRestaurantsOperationsEnabled())
        {
            throw ApiException.NotFound("Restaurant operations are not enabled.");
        }
    }
}
