// HTTP endpoints for profile resources.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/profiles")]
/// <summary>
/// Manages profile retrieval and updates.
/// </summary>
public sealed class ProfilesController(
    ProfileService profileService,
    MediaService mediaService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("me")]
    public Task<ProfileDto> GetMyProfile(CancellationToken cancellationToken) =>
        profileService.GetMyProfileAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, cancellationToken);

    /// <summary>
    /// Returns a public-facing profile summary for any user by ID.
    /// Used by the user profile pop-up on budz, group members, and event participants.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public Task<ProfileDto> GetProfile(Guid userId, CancellationToken cancellationToken) =>
        profileService.GetProfileAsync(userId, cancellationToken);

    [HttpPatch("me")]
    public Task<ProfileDto> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken) =>
        profileService.UpdateMyProfileAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, request, cancellationToken);

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    public Task<MediaAssetDto> UploadMyAvatar([FromForm] UploadImageRequest request, CancellationToken cancellationToken) =>
        mediaService.UploadProfileAvatarAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
