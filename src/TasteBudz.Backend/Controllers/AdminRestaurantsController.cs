using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/restaurants")]
public sealed class AdminRestaurantsController(
    RestaurantCatalogAdminService restaurantCatalogAdminService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<AdminRestaurantCatalogItemDto>> List(CancellationToken cancellationToken) =>
        restaurantCatalogAdminService.ListAsync(currentUserAccessor.GetRequiredCurrentUser(), cancellationToken);

    [HttpGet("search")]
    public Task<ListResponse<AdminRestaurantCatalogItemDto>> Search(
        [FromQuery] AdminRestaurantSearchQuery query,
        CancellationToken cancellationToken) =>
        restaurantCatalogAdminService.SearchAsync(currentUserAccessor.GetRequiredCurrentUser(), query, cancellationToken);

    [HttpPost]
    public Task<AdminRestaurantCatalogItemDto> Create(
        [FromBody] SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken) =>
        restaurantCatalogAdminService.CreateAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);

    [HttpPatch("{restaurantId:guid}")]
    public Task<AdminRestaurantCatalogItemDto> Update(
        Guid restaurantId,
        [FromBody] SaveRestaurantCatalogRequest request,
        CancellationToken cancellationToken) =>
        restaurantCatalogAdminService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, request, cancellationToken);

    [HttpPost("{restaurantId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid restaurantId, CancellationToken cancellationToken)
    {
        await restaurantCatalogAdminService.ArchiveAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{restaurantId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid restaurantId, CancellationToken cancellationToken)
    {
        await restaurantCatalogAdminService.RestoreAsync(currentUserAccessor.GetRequiredCurrentUser(), restaurantId, cancellationToken);
        return NoContent();
    }
}
