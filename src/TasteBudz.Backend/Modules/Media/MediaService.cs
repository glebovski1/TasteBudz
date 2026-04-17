using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Media;

/// <summary>
/// Owns image validation, storage, and access checks for media assets.
/// </summary>
public sealed class MediaService(
    IMediaRepository mediaRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IModerationRepository moderationRepository,
    IEventFeedbackAccessService eventFeedbackAccessService,
    IClock clock)
{
    public async Task<MediaAssetDto> UploadProfileAvatarAsync(CurrentUser currentUser, UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        _ = await authRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw ApiException.NotFound("The current account could not be found.");
        _ = await profileRepository.GetProfileAsync(currentUser.UserId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");

        var file = await ImageUploadValidator.ReadValidatedImageAsync(request, cancellationToken);
        var existingAvatar = await mediaRepository.GetProfileAvatarAsync(currentUser.UserId, cancellationToken);

        if (existingAvatar is not null)
        {
            await mediaRepository.DeleteAsync(existingAvatar.Id, cancellationToken);
        }

        var mediaAsset = new MediaAsset(
            Guid.NewGuid(),
            currentUser.UserId,
            currentUser.UserId,
            null,
            null,
            null,
            file.FileName,
            file.ContentType,
            file.Content.LongLength,
            file.Content,
            clock.UtcNow);

        await mediaRepository.SaveAsync(mediaAsset, cancellationToken);
        return ToDto(mediaAsset);
    }

    public async Task<MediaAssetDto> UploadReportAttachmentAsync(CurrentUser currentUser, Guid reportId, UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        var report = await moderationRepository.GetReportAsync(reportId, cancellationToken)
            ?? throw ApiException.NotFound("The requested report could not be found.");

        if (report.ReporterUserId != currentUser.UserId)
        {
            throw ApiException.Forbidden("Only the reporting user can attach evidence to this report.");
        }

        if (report.Status == ModerationReportStatus.Resolved)
        {
            throw ApiException.Conflict("Resolved reports cannot accept new evidence attachments.");
        }

        var file = await ImageUploadValidator.ReadValidatedImageAsync(request, cancellationToken);
        var mediaAsset = new MediaAsset(
            Guid.NewGuid(),
            currentUser.UserId,
            null,
            null,
            null,
            reportId,
            file.FileName,
            file.ContentType,
            file.Content.LongLength,
            file.Content,
            clock.UtcNow);

        await mediaRepository.SaveAsync(mediaAsset, cancellationToken);
        return ToDto(mediaAsset);
    }

    public async Task<IReadOnlyCollection<MediaAssetDto>> ListReportAttachmentsAsync(CurrentUser currentUser, Guid reportId, CancellationToken cancellationToken = default)
    {
        await AuthorizeReportAttachmentAccessAsync(currentUser, reportId, cancellationToken);
        var attachments = await mediaRepository.ListReportAttachmentsAsync(reportId, cancellationToken);
        return attachments.Select(ToDto).ToArray();
    }

    public async Task<MediaAsset> GetContentAsync(CurrentUser currentUser, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var mediaAsset = await mediaRepository.GetAsync(mediaAssetId, cancellationToken)
            ?? throw ApiException.NotFound("The requested media asset could not be found.");

        if (mediaAsset.ProfileUserId.HasValue)
        {
            return mediaAsset;
        }

        if (mediaAsset.ReportId.HasValue)
        {
            await AuthorizeReportAttachmentAccessAsync(currentUser, mediaAsset.ReportId.Value, cancellationToken);
            return mediaAsset;
        }

        if (mediaAsset.EventId.HasValue)
        {
            var photo = await eventFeedbackAccessService.GetFeedbackPhotoByMediaAssetAsync(mediaAsset.Id, cancellationToken);

            if (photo is not null &&
                await eventFeedbackAccessService.CanViewFeedbackAsync(currentUser, mediaAsset.EventId.Value, cancellationToken))
            {
                return mediaAsset;
            }
        }

        throw ApiException.Forbidden("The requested media asset is not available through this endpoint.");
    }

    private async Task AuthorizeReportAttachmentAccessAsync(CurrentUser currentUser, Guid reportId, CancellationToken cancellationToken)
    {
        var report = await moderationRepository.GetReportAsync(reportId, cancellationToken)
            ?? throw ApiException.NotFound("The requested report could not be found.");

        if (report.ReporterUserId == currentUser.UserId)
        {
            return;
        }

        if (currentUser.IsInRole(UserRole.Moderator) || currentUser.IsInRole(UserRole.Admin))
        {
            return;
        }

        throw ApiException.Forbidden("You are not allowed to access attachments for this report.");
    }

    private static MediaAssetDto ToDto(MediaAsset mediaAsset) =>
        new(
            mediaAsset.Id,
            mediaAsset.OriginalFileName,
            mediaAsset.ContentType,
            mediaAsset.ContentLength,
            mediaAsset.CreatedAtUtc);
}
