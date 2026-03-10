// HTTP endpoints for user preference retrieval and replacement.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/preferences")]
/// <summary>
/// Manages the authenticated user's saved food preferences.
/// </summary>
public sealed class PreferencesController(PreferenceService preferenceService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("me")]
    public Task<PreferenceDto> GetMyPreferences(CancellationToken cancellationToken) =>
        preferenceService.GetAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPut("me")]
    public Task<PreferenceDto> ReplaceMyPreferences([FromBody] ReplacePreferencesRequest request, CancellationToken cancellationToken) =>
        preferenceService.ReplaceAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, request, cancellationToken);
}