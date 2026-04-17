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
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Media;

public sealed class MediaServiceTests
{
    [Fact]
    public async Task UploadProfileAvatarAsync_ReplacesThePreviousAvatar()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var mediaRepository = new InMemoryMediaRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var userId = Guid.NewGuid();

        await authRepository.CreateAccountAsync(new UserAccount(userId, "alex", "ALEX", "alex@example.com", "ALEX@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null));
        await profileRepository.SaveProfileAsync(new UserProfile(userId, "Alex", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));

        var service = new MediaService(mediaRepository, authRepository, profileRepository, moderationRepository, new DenyingEventFeedbackAccessService(), clock);
        var currentUser = new CurrentUser(userId, "alex", new[] { UserRole.User });

        var first = await service.UploadProfileAvatarAsync(currentUser, CreateUploadRequest("first.png", "image/png", new byte[] { 1, 2, 3 }));
        var second = await service.UploadProfileAvatarAsync(currentUser, CreateUploadRequest("second.png", "image/png", new byte[] { 4, 5, 6, 7 }));

        var avatar = await mediaRepository.GetProfileAvatarAsync(userId);

        Assert.NotEqual(first.MediaAssetId, second.MediaAssetId);
        Assert.Equal(second.MediaAssetId, avatar!.Id);
        Assert.Null(await mediaRepository.GetAsync(first.MediaAssetId));
    }

    [Fact]
    public async Task UploadProfileAvatarAsync_WithUnsupportedContentType_ReturnsBadRequest()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var mediaRepository = new InMemoryMediaRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var userId = Guid.NewGuid();

        await authRepository.CreateAccountAsync(new UserAccount(userId, "alex", "ALEX", "alex@example.com", "ALEX@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null));
        await profileRepository.SaveProfileAsync(new UserProfile(userId, "Alex", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));

        var service = new MediaService(mediaRepository, authRepository, profileRepository, moderationRepository, new DenyingEventFeedbackAccessService(), clock);
        var currentUser = new CurrentUser(userId, "alex", new[] { UserRole.User });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadProfileAvatarAsync(currentUser, CreateUploadRequest("notes.txt", "text/plain", new byte[] { 1, 2, 3 })));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task UploadReportAttachmentAsync_WithNonReporter_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 4, 10, 14, 0, 0, TimeSpan.Zero));
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var mediaRepository = new InMemoryMediaRepository(store);
        var moderationRepository = new InMemoryModerationRepository(store);
        var reporterId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        await authRepository.CreateAccountAsync(new UserAccount(reporterId, "reporter", "REPORTER", "reporter@example.com", "REPORTER@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null));
        await authRepository.CreateAccountAsync(new UserAccount(outsiderId, "outsider", "OUTSIDER", "outsider@example.com", "OUTSIDER@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null));
        await authRepository.CreateAccountAsync(new UserAccount(subjectId, "subject", "SUBJECT", "subject@example.com", "SUBJECT@EXAMPLE.COM", "hash", AccountStatus.Active, new[] { UserRole.User }, clock.UtcNow, clock.UtcNow, null));
        await profileRepository.SaveProfileAsync(new UserProfile(reporterId, "Reporter", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));
        await profileRepository.SaveProfileAsync(new UserProfile(outsiderId, "Outsider", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));
        await profileRepository.SaveProfileAsync(new UserProfile(subjectId, "Subject", null, "45220", SocialGoal.Friends, clock.UtcNow, clock.UtcNow));
        await moderationRepository.SaveReportAsync(new ModerationReport(reportId, reporterId, ReportTargetType.User, subjectId, "Harassment", "Repeated abuse", null, null, subjectId, null, clock.UtcNow, ModerationReportStatus.Pending, null, null, null, null));

        var service = new MediaService(mediaRepository, authRepository, profileRepository, moderationRepository, new DenyingEventFeedbackAccessService(), clock);
        var currentUser = new CurrentUser(outsiderId, "outsider", new[] { UserRole.User });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UploadReportAttachmentAsync(currentUser, reportId, CreateUploadRequest("proof.png", "image/png", new byte[] { 9, 8, 7 })));

        Assert.Equal(403, exception.StatusCode);
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

    private sealed class DenyingEventFeedbackAccessService : IEventFeedbackAccessService
    {
        public Task<bool> CanViewFeedbackAsync(CurrentUser currentUser, Guid eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<EventFeedbackPhoto?> GetFeedbackPhotoByMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventFeedbackPhoto?>(null);
    }
}
