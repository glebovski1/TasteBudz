// HTTP endpoints for privacy settings that currently affect discovery visibility.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/privacy-settings")]
/// <summary>
/// Reads and updates the authenticated user's privacy settings.
/// </summary>
public sealed class PrivacySettingsController(PrivacyService privacyService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("me")]
    public Task<PrivacySettingsDto> Get(CancellationToken cancellationToken) =>
        privacyService.GetAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPatch("me")]
    public Task<PrivacySettingsDto> Update([FromBody] UpdatePrivacySettingsRequest request, CancellationToken cancellationToken) =>
        privacyService.UpdateAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, request, cancellationToken);
}