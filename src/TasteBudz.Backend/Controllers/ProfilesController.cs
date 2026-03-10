// HTTP endpoints for the authenticated user's profile resource.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/profiles")]
/// <summary>
/// Manages the current user's profile fields.
/// </summary>
public sealed class ProfilesController(ProfileService profileService, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("me")]
    public Task<ProfileDto> GetMyProfile(CancellationToken cancellationToken) =>
        profileService.GetMyProfileAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    [HttpPatch("me")]
    public Task<ProfileDto> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken) =>
        profileService.UpdateMyProfileAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, request, cancellationToken);
}