using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize]
[Route("media")]
public sealed class MediaController : Controller
{
    private readonly BackendHttpClient backendHttpClient;

    public MediaController(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    [HttpGet("{mediaAssetId:guid}")]
    public async Task<IActionResult> Get(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        try
        {
            var media = await backendHttpClient.GetFileAsync($"/api/v1/media/{mediaAssetId}", cancellationToken);
            return File(media.Content, media.ContentType);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return Unauthorized();
        }
        catch (BackendApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (BackendApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new { message = ex.Message });
        }
    }
}
