using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Events;

public sealed class EventFeedbackServiceTests
{
    [Fact]
    public async Task UpsertMineAsync_WithActiveEvent_ReturnsConflict()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Open);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
            {
                Rating = 5,
                Text = "Great time.",
            }));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task UpsertMineAsync_ForCompletedEvent_TrimsTextAndUpdatesOneEntry()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Completed);

        var first = await services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 4,
            Text = "  Good coordination.  ",
        });

        clock.Advance(TimeSpan.FromMinutes(5));
        var second = await services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Excellent table and group.",
        });

        Assert.Equal(first.FeedbackId, second.FeedbackId);
        Assert.Equal(5, second.Rating);
        Assert.Equal("Excellent table and group.", second.Text);
        Assert.True(second.UpdatedAtUtc > first.UpdatedAtUtc);
        Assert.Single(services.Store.EventFeedbacks.Values, feedback => feedback.EventId == eventId && feedback.AuthorUserId == host.UserId);
    }

    [Fact]
    public async Task UpsertMineAsync_WithInvalidTextOrRating_ReturnsBadRequest()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Completed);

        var blankText = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
            {
                Rating = 5,
                Text = " ",
            }));
        var badRating = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
            {
                Rating = 6,
                Text = "Good group.",
            }));

        Assert.Equal(400, blankText.StatusCode);
        Assert.Equal(400, badRating.StatusCode);
    }

    [Fact]
    public async Task ListAsync_ForClosedEvent_RequiresParticipantHostOrModerator()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var guest = await services.CreateUserAsync("guest");
        var outsider = await services.CreateUserAsync("outsider");
        var moderator = await services.CreateUserAsync("mod", new[] { UserRole.Moderator });
        var eventId = await services.SeedEventAsync(host, EventType.Closed, EventStatus.Completed, guest);

        await services.FeedbackService.UpsertMineAsync(guest, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Glad I joined.",
        });

        var outsiderException = await Assert.ThrowsAsync<ApiException>(() => services.FeedbackService.ListAsync(outsider, eventId));
        var guestFeedback = await services.FeedbackService.ListAsync(guest, eventId);
        var hostFeedback = await services.FeedbackService.ListAsync(host, eventId);
        var moderatorFeedback = await services.FeedbackService.ListAsync(moderator, eventId);

        Assert.Equal(403, outsiderException.StatusCode);
        Assert.Single(guestFeedback);
        Assert.Single(hostFeedback);
        Assert.Single(moderatorFeedback);
    }

    [Fact]
    public async Task ListAsync_ForOpenCompletedEvent_UsesEventVisibilityAfterEventEnds()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var guest = await services.CreateUserAsync("guest");
        var outsider = await services.CreateUserAsync("outsider");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Completed, guest);
        await services.ProfileRepository.SavePrivacySettingsAsync(new PrivacySettings(host.UserId, false, clock.UtcNow));

        await services.FeedbackService.UpsertMineAsync(guest, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Glad I joined.",
        });

        var outsiderException = await Assert.ThrowsAsync<ApiException>(() => services.FeedbackService.ListAsync(outsider, eventId));
        var guestFeedback = await services.FeedbackService.ListAsync(guest, eventId);

        Assert.Equal(403, outsiderException.StatusCode);
        Assert.Single(guestFeedback);
    }

    [Fact]
    public async Task UploadMyPhotoAsync_RequiresFeedbackAndEnforcesPhotoLimit()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Completed);

        var missingFeedback = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.UploadMyPhotoAsync(host, eventId, CreateUploadRequest("first.png", "image/png", new byte[] { 1, 2, 3 })));

        await services.FeedbackService.UpsertMineAsync(host, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Loved it.",
        });

        for (var index = 0; index < 4; index++)
        {
            await services.FeedbackService.UploadMyPhotoAsync(host, eventId, CreateUploadRequest($"photo-{index}.png", "image/png", new byte[] { 1, 2, 3 }));
        }

        var fifth = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.UploadMyPhotoAsync(host, eventId, CreateUploadRequest("extra.png", "image/png", new byte[] { 4, 5, 6 })));

        Assert.Equal(409, missingFeedback.StatusCode);
        Assert.Equal(409, fifth.StatusCode);
        Assert.Equal(4, services.Store.EventFeedbackPhotos.Count);
        Assert.Equal(4, services.Store.MediaAssets.Count);
    }

    [Fact]
    public async Task DeleteMyPhotoAsync_WithAnotherAuthorsPhoto_ReturnsNotFound()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var guest = await services.CreateUserAsync("guest");
        var eventId = await services.SeedEventAsync(host, EventType.Open, EventStatus.Completed, guest);

        await services.FeedbackService.UpsertMineAsync(guest, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Great dinner.",
        });
        var photo = await services.FeedbackService.UploadMyPhotoAsync(guest, eventId, CreateUploadRequest("guest.png", "image/png", new byte[] { 1, 2, 3 }));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.FeedbackService.DeleteMyPhotoAsync(host, eventId, photo.MediaAssetId));

        Assert.Equal(404, exception.StatusCode);
        Assert.True(services.Store.MediaAssets.ContainsKey(photo.MediaAssetId));
    }

    [Fact]
    public async Task GetContentAsync_ForFeedbackPhoto_UsesClosedEventFeedbackVisibility()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 17, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var host = await services.CreateUserAsync("host");
        var guest = await services.CreateUserAsync("guest");
        var outsider = await services.CreateUserAsync("outsider");
        var eventId = await services.SeedEventAsync(host, EventType.Closed, EventStatus.Completed, guest);

        await services.FeedbackService.UpsertMineAsync(guest, eventId, new UpsertEventFeedbackRequest
        {
            Rating = 5,
            Text = "Private dinner feedback.",
        });
        var photo = await services.FeedbackService.UploadMyPhotoAsync(guest, eventId, CreateUploadRequest("closed.webp", "image/webp", new byte[] { 1, 2, 3 }));

        var outsiderException = await Assert.ThrowsAsync<ApiException>(() => services.MediaService.GetContentAsync(outsider, photo.MediaAssetId));
        var content = await services.MediaService.GetContentAsync(host, photo.MediaAssetId);

        Assert.Equal(403, outsiderException.StatusCode);
        Assert.Equal(photo.MediaAssetId, content.Id);
    }

    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var feedbackRepository = new InMemoryEventFeedbackRepository(store);
        var mediaRepository = new InMemoryMediaRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var feedbackService = new EventFeedbackService(eventRepository, feedbackRepository, mediaRepository, authRepository, profileRepository, lifecycleService, clock);
        var mediaService = new MediaService(mediaRepository, authRepository, profileRepository, moderationRepository, feedbackService, clock);

        return new TestServices(store, authRepository, profileRepository, eventRepository, feedbackService, mediaService, clock);
    }

    private static UploadImageRequest CreateUploadRequest(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
        };

        file.Headers[HeaderNames.ContentType] = contentType;
        return new UploadImageRequest { File = file };
    }

    private sealed record TestServices(
        InMemoryTasteBudzStore Store,
        IAuthRepository AuthRepository,
        IProfileRepository ProfileRepository,
        IEventRepository EventRepository,
        EventFeedbackService FeedbackService,
        MediaService MediaService,
        TestClock Clock)
    {
        public async Task<CurrentUser> CreateUserAsync(string username, IReadOnlyCollection<UserRole>? roles = null)
        {
            var userId = Guid.NewGuid();
            var effectiveRoles = roles ?? new[] { UserRole.User };
            await AuthRepository.CreateAccountAsync(new UserAccount(
                userId,
                username,
                username.ToUpperInvariant(),
                $"{username}@example.com",
                $"{username.ToUpperInvariant()}@EXAMPLE.COM",
                "hash",
                AccountStatus.Active,
                effectiveRoles,
                Clock.UtcNow,
                Clock.UtcNow,
                null));
            await ProfileRepository.SaveProfileAsync(new UserProfile(userId, username, null, "45220", SocialGoal.Friends, Clock.UtcNow, Clock.UtcNow));

            return new CurrentUser(userId, username, effectiveRoles);
        }

        public async Task<Guid> SeedEventAsync(CurrentUser host, EventType eventType, EventStatus status, params CurrentUser[] additionalJoinedUsers)
        {
            var eventId = Guid.NewGuid();
            await EventRepository.SaveAsync(new Event(
                eventId,
                host.UserId,
                "Feedback dinner",
                eventType,
                status,
                Clock.UtcNow.AddHours(status == EventStatus.Completed ? -2 : 2),
                Clock.UtcNow.AddHours(status == EventStatus.Completed ? -3 : 1),
                6,
                2,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                null,
                null,
                null,
                Clock.UtcNow.AddDays(-1),
                Clock.UtcNow,
                null,
                status == EventStatus.Completed ? Clock.UtcNow : null));
            await EventRepository.SaveParticipantAsync(new EventParticipant(eventId, host.UserId, EventParticipantState.Joined, null, Clock.UtcNow.AddDays(-1), Clock.UtcNow.AddDays(-1), null, null));

            foreach (var user in additionalJoinedUsers)
            {
                await EventRepository.SaveParticipantAsync(new EventParticipant(eventId, user.UserId, EventParticipantState.Joined, null, Clock.UtcNow.AddDays(-1), Clock.UtcNow.AddDays(-1), null, null));
            }

            return eventId;
        }
    }
}
