using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// JSON endpoints powering the user profile pop-up card.
/// All actions return JSON so the modal can call them via fetch() without a page reload.
/// </summary>
[Authorize]
[Route("[controller]/[action]")]
public sealed class UserCardController : Controller
{
    private readonly ProfileApiService profileApiService;
    private readonly ModerationApiService moderationApiService;
    private readonly UserSessionService userSessionService;

    public UserCardController(
        ProfileApiService profileApiService,
        ModerationApiService moderationApiService,
        UserSessionService userSessionService)
    {
        this.profileApiService = profileApiService;
        this.moderationApiService = moderationApiService;
        this.userSessionService = userSessionService;
    }

    // GET /UserCard/Profile/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Profile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileApiService.GetPublicProfileAsync(id, cancellationToken);
            var isBud = await profileApiService.IsBudAsync(id, cancellationToken);

            return Ok(new
            {
                userId = profile.UserId,
                username = profile.Username,
                displayName = profile.DisplayName,
                bio = profile.Bio,
                socialGoal = profile.SocialGoal?.ToString(),
                zipCode = profile.HomeAreaZipCode,
                isBud,
            });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return Unauthorized();
        }
        catch
        {
            return NotFound(new { error = "User not found." });
        }
    }

    // POST /UserCard/UnBud
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnBud([FromBody] UserIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await profileApiService.RemoveBudAsync(request.UserId, cancellationToken);
            return Ok(new { success = true });
        }
        catch (BackendAuthenticationExpiredException) { return Unauthorized(); }
        catch (BackendApiException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // POST /UserCard/Block
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block([FromBody] UserIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await profileApiService.CreateBlockAsync(
                new CreateBlockRequest { BlockedUserId = request.UserId },
                cancellationToken);
            return Ok(new { success = true });
        }
        catch (BackendAuthenticationExpiredException) { return Unauthorized(); }
        catch (BackendApiException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // POST /UserCard/Report
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report([FromBody] ReportRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "A reason is required." });

        try
        {
            await moderationApiService.CreateReportAsync(
                new CreateModerationReportRequest
                {
                    TargetType = ReportTargetType.User,
                    TargetId = request.UserId,
                    Category = request.Category ?? "General",
                    Reason = request.Reason.Trim(),
                    Explanation = request.Explanation?.Trim(),
                },
                cancellationToken);

            return Ok(new { success = true });
        }
        catch (BackendAuthenticationExpiredException) { return Unauthorized(); }
        catch (BackendApiException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
