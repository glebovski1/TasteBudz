using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Serves the BudzSwipe discovery page and handles swipe decisions
/// posted from the page's JavaScript layer.
/// </summary>
[Authorize]
public sealed class DiscoveryController : Controller
{
    private readonly DiscoveryApiService discoveryApiService;
    private readonly UserSessionService userSessionService;

    public DiscoveryController(
        DiscoveryApiService discoveryApiService,
        UserSessionService userSessionService)
    {
        this.discoveryApiService = discoveryApiService;
        this.userSessionService  = userSessionService;
    }

    // GET /Discovery/Swipe
    [HttpGet]
    public async Task<IActionResult> Swipe(CancellationToken cancellationToken)
    {
        try
        {
            var result = await discoveryApiService.GetSwipeCandidatesAsync(
                new SwipeCandidatesQuery { PageSize = 10 },
                cancellationToken);

            return View(SwipeViewModel.FromDto(result.Items));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
    }

    // POST /Discovery/RecordSwipe
    // Called by fetch() in the view's script — returns JSON, not a redirect.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSwipe(
        [FromBody] RecordSwipeDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await discoveryApiService.RecordSwipeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (BackendAuthenticationExpiredException)
        {
            // Tell the JS layer the session expired so it can redirect the user.
            return Unauthorized(new { error = "Session expired. Please log in again." });
        }
        catch (BackendApiException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
