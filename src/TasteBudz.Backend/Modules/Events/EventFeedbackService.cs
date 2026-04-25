using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Owns event feedback eligibility, visibility, and photo attachment workflows.
/// </summary>
public sealed class EventFeedbackService(
    IEventRepository eventRepository,
    IEventFeedbackRepository feedbackRepository,
    IMediaRepository mediaRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    EventLifecycleService lifecycleService,
    IClock clock,
    IPersistenceTransactionRunner? transactionRunner = null) : IEventFeedbackAccessService
{
    private const int MaxPhotosPerFeedback = 4;
    private const int MaxFeedbackTextLength = 1000;
    private readonly IPersistenceTransactionRunner persistenceTransactionRunner = transactionRunner ?? NoOpPersistenceTransactionRunner.Instance;

    public async Task<IReadOnlyCollection<EventFeedbackDto>> ListAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default)
    {
        if (!await CanViewFeedbackAsync(currentUser, eventId, cancellationToken))
        {
            throw ApiException.Forbidden("You are not allowed to view feedback for this event.");
        }

        var feedbackItems = await feedbackRepository.ListForEventAsync(eventId, cancellationToken);
        var items = new List<EventFeedbackDto>(feedbackItems.Count);

        foreach (var feedback in feedbackItems)
        {
            items.Add(await MapAsync(feedback, cancellationToken));
        }

        return items;
    }

    public async Task<EventFeedbackDto> UpsertMineAsync(CurrentUser currentUser, Guid eventId, UpsertEventFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        await EnsureCanSubmitAsync(currentUser.UserId, eventRecord, cancellationToken);

        var rating = request.Rating ?? throw ApiException.BadRequest("rating is required.");
        EnsureRatingInRange(rating);
        var text = NormalizeText(request.Text);
        var now = clock.UtcNow;
        var existing = await feedbackRepository.GetForAuthorAsync(eventId, currentUser.UserId, cancellationToken);

        var feedback = existing is null
            ? new EventFeedback(Guid.NewGuid(), eventId, currentUser.UserId, rating, text, now, now)
            : existing with
            {
                Rating = rating,
                Text = text,
                UpdatedAtUtc = now,
            };

        await feedbackRepository.SaveAsync(feedback, cancellationToken);
        return await MapAsync(feedback, cancellationToken);
    }

    public async Task<EventFeedbackPhotoDto> UploadMyPhotoAsync(CurrentUser currentUser, Guid eventId, UploadImageRequest request, CancellationToken cancellationToken = default)
    {
        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        await EnsureCanSubmitAsync(currentUser.UserId, eventRecord, cancellationToken);

        var feedback = await feedbackRepository.GetForAuthorAsync(eventId, currentUser.UserId, cancellationToken)
            ?? throw ApiException.Conflict("Submit feedback before adding photos.");
        var photos = await feedbackRepository.ListPhotosForFeedbackAsync(feedback.Id, cancellationToken);

        if (photos.Count >= MaxPhotosPerFeedback)
        {
            throw ApiException.Conflict($"Feedback can have at most {MaxPhotosPerFeedback} photos.");
        }

        var file = await ImageUploadValidator.ReadValidatedImageAsync(request, cancellationToken);
        var mediaAsset = new MediaAsset(
            Guid.NewGuid(),
            currentUser.UserId,
            null,
            null,
            eventId,
            null,
            file.FileName,
            file.ContentType,
            file.Content.LongLength,
            file.Content,
            clock.UtcNow);
        var photo = new EventFeedbackPhoto(feedback.Id, mediaAsset.Id, mediaAsset.CreatedAtUtc);

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await mediaRepository.SaveAsync(mediaAsset, cancellationToken);
                await feedbackRepository.SavePhotoAsync(photo, cancellationToken);
            },
            cancellationToken);

        return EventFeedbackMapper.ToPhoto(mediaAsset);
    }

    public async Task DeleteMyPhotoAsync(CurrentUser currentUser, Guid eventId, Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        _ = await GetSynchronizedEventAsync(eventId, cancellationToken);
        var feedback = await feedbackRepository.GetForAuthorAsync(eventId, currentUser.UserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested feedback photo could not be found.");
        var photo = await feedbackRepository.GetPhotoAsync(feedback.Id, mediaAssetId, cancellationToken)
            ?? throw ApiException.NotFound("The requested feedback photo could not be found.");
        var mediaAsset = await mediaRepository.GetAsync(photo.MediaAssetId, cancellationToken)
            ?? throw ApiException.NotFound("The requested feedback photo could not be found.");

        if (mediaAsset.OwnerUserId != currentUser.UserId || mediaAsset.EventId != eventId)
        {
            throw ApiException.NotFound("The requested feedback photo could not be found.");
        }

        await persistenceTransactionRunner.ExecuteAsync(
            async () =>
            {
                await feedbackRepository.DeletePhotoAsync(feedback.Id, mediaAssetId, cancellationToken);
                await mediaRepository.DeleteAsync(mediaAssetId, cancellationToken);
            },
            cancellationToken);
    }

    public async Task<bool> CanViewFeedbackAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventRecord = await GetSynchronizedEventAsync(eventId, cancellationToken);
        var isPrivileged = currentUser.IsInRole(UserRole.Moderator) || currentUser.IsInRole(UserRole.Admin);

        if (!await EventVisibilityPolicy.CanViewAsync(
                currentUser.UserId,
                isPrivileged,
                eventRecord,
                eventRepository,
                profileRepository,
                cancellationToken))
        {
            return false;
        }

        if (eventRecord.EventType == EventType.Open)
        {
            return true;
        }

        if (eventRecord.HostUserId == currentUser.UserId || isPrivileged)
        {
            return true;
        }

        var participant = await eventRepository.GetParticipantAsync(eventId, currentUser.UserId, cancellationToken);
        return participant?.State == EventParticipantState.Joined;
    }

    public Task<EventFeedbackPhoto?> GetFeedbackPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
        feedbackRepository.GetPhotoByMediaAssetAsync(mediaAssetId, cancellationToken);

    private async Task<EventFeedbackDto> MapAsync(EventFeedback feedback, CancellationToken cancellationToken)
    {
        var account = await authRepository.GetByIdAsync(feedback.AuthorUserId, cancellationToken)
            ?? throw ApiException.NotFound("The feedback author could not be found.");
        var profile = await profileRepository.GetProfileAsync(feedback.AuthorUserId, cancellationToken);
        var photoLinks = await feedbackRepository.ListPhotosForFeedbackAsync(feedback.Id, cancellationToken);
        var photos = new List<EventFeedbackPhotoDto>(photoLinks.Count);

        foreach (var photo in photoLinks)
        {
            var mediaAsset = await mediaRepository.GetAsync(photo.MediaAssetId, cancellationToken);

            if (mediaAsset is not null)
            {
                photos.Add(EventFeedbackMapper.ToPhoto(mediaAsset));
            }
        }

        return EventFeedbackMapper.ToDto(feedback, account, profile, photos);
    }

    private async Task<Event> GetSynchronizedEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var eventRecord = await eventRepository.GetAsync(eventId, cancellationToken)
            ?? throw ApiException.NotFound("The requested event could not be found.");

        return await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken);
    }

    private async Task EnsureCanSubmitAsync(Guid currentUserId, Event eventRecord, CancellationToken cancellationToken)
    {
        if (eventRecord.Status != EventStatus.Completed)
        {
            throw ApiException.Conflict("Feedback can only be submitted after the event is completed.");
        }

        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);

        if (participant?.State != EventParticipantState.Joined)
        {
            throw ApiException.Forbidden("Only joined event participants can submit feedback.");
        }
    }

    private static void EnsureRatingInRange(int rating)
    {
        if (rating is < 1 or > 5)
        {
            throw ApiException.BadRequest("rating must be between 1 and 5.");
        }
    }

    private static string NormalizeText(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value)
            ? throw ApiException.BadRequest("text is required.")
            : value.Trim();

        if (text.Length > MaxFeedbackTextLength)
        {
            throw ApiException.BadRequest($"text must be {MaxFeedbackTextLength} characters or fewer.");
        }

        return text;
    }
}
