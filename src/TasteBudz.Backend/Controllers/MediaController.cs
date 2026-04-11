// HTTP endpoints for reading stored media asset content.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Media;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/media")]
/// <summary>
/// Serves authorized media content stored in the application database.
/// </summary>
public sealed class MediaController(
    MediaService mediaService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("{mediaAssetId:guid}")]
    public async Task<IActionResult> Get(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var mediaAsset = await mediaService.GetContentAsync(currentUserAccessor.GetRequiredCurrentUser(), mediaAssetId, cancellationToken);
        return File(mediaAsset.Content, mediaAsset.ContentType);
    }
}
