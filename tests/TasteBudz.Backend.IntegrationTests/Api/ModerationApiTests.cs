// Integration tests for moderation, restrictions, and audit APIs.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Exercises end-user report submission and moderator/admin review flows through HTTP.
/// </summary>
public sealed class ModerationApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task ModerationSearch_AllowsAdminAndModeratorButDeniesPlainUser()
    {
        factory.ResetState();
        using var adminClient = factory.CreateClient();
        using var moderatorClient = factory.CreateClient();
        using var userClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var adminSession = await ApiTestHelpers.RegisterAsync(adminClient, username: "admin", email: "admin@example.com");
        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var userSession = await ApiTestHelpers.RegisterAsync(userClient, username: "plain", email: "plain@example.com");
        _ = await ApiTestHelpers.RegisterAsync(subjectClient, username: "searchsubject", email: "searchsubject@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, adminSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(adminClient, adminSession.AccessToken);
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(userClient, userSession.AccessToken);

        var adminResponse = await adminClient.GetAsync("/api/v1/moderation/search?q=searchsubject&pageSize=20");
        var moderatorResponse = await moderatorClient.GetAsync("/api/v1/moderation/search?q=searchsubject&pageSize=20");
        var userResponse = await userClient.GetAsync("/api/v1/moderation/search?q=searchsubject&pageSize=20");
        var adminSearch = await adminResponse.Content.ReadFromJsonAsync<ModerationSearchResponseDto>(ApiTestHelpers.JsonOptions);
        var moderatorSearch = await moderatorResponse.Content.ReadFromJsonAsync<ModerationSearchResponseDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, moderatorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
        Assert.Contains(adminSearch!.Items, item => item.Kind == ModerationSearchResultKind.User && item.PrimaryUser?.Username == "searchsubject");
        Assert.Contains(moderatorSearch!.Items, item => item.Kind == ModerationSearchResultKind.User && item.PrimaryUser?.Username == "searchsubject");
    }

    [Fact]
    public async Task ModerationSearch_WhenMessageBodyMatches_ReturnsSenderIdentity()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var senderClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var senderSession = await ApiTestHelpers.RegisterAsync(senderClient, username: "sender", email: "sender@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(senderClient, senderSession.AccessToken);

        var messageResponse = await senderClient.PostAsJsonAsync("/api/v1/support/messages", new SendSupportMessageRequest
        {
            Body = "needle escalation message for moderator search",
        });

        var searchResponse = await moderatorClient.GetAsync("/api/v1/moderation/search?q=needle&type=Message&pageSize=20");
        var search = await searchResponse.Content.ReadFromJsonAsync<ModerationSearchResponseDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, messageResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var message = Assert.Single(search!.Items, item => item.Kind == ModerationSearchResultKind.Message);
        Assert.Equal("sender", message.PrimaryUser!.Username);
        Assert.Contains("needle escalation message", message.Snippet);
    }

    [Fact]
    public async Task ModerationSearch_WhenQueryBlankAndTypeUser_ReturnsAllUsers()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "searchsubject", email: "searchsubject@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);

        var searchResponse = await moderatorClient.GetAsync("/api/v1/moderation/search?type=User&pageSize=20");
        var search = await searchResponse.Content.ReadFromJsonAsync<ModerationSearchResponseDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Contains(search!.Items, item =>
            item.Kind == ModerationSearchResultKind.User &&
            item.PrimaryUser?.UserId == subjectSession.CurrentUser.UserId);
    }

    [Fact]
    public async Task ReportReview_WhenUserReport_ResolvesReporterAndSubjectUserLinks()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);

        var reportResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Safety",
            Reason = "Readable links required",
        });
        var report = await reportResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var reviewResponse = await moderatorClient.GetAsync($"/api/v1/moderation/reports/{report!.ReportId}/review");
        var review = await reviewResponse.Content.ReadFromJsonAsync<ModerationReportReviewDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        Assert.Equal("reporter", review!.Reporter!.Username);
        Assert.Equal("subject", review.SubjectUser!.Username);
        Assert.Equal(subjectSession.CurrentUser.UserId, review.SubjectUser.UserId);
    }

    [Fact]
    public async Task ReportReview_WhenMessageReport_UsesMessageSenderAsSubjectUser()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var senderClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var senderSession = await ApiTestHelpers.RegisterAsync(senderClient, username: "sender", email: "sender@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);
        ApiTestHelpers.SetBearer(senderClient, senderSession.AccessToken);

        var messageResponse = await senderClient.PostAsJsonAsync("/api/v1/support/messages", new SendSupportMessageRequest
        {
            Body = "message that should resolve to sender",
        });
        var message = await messageResponse.Content.ReadFromJsonAsync<ChatMessageDto>(ApiTestHelpers.JsonOptions);
        var reportResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.Message,
            TargetId = message!.MessageId,
            Category = "Harassment",
            Reason = "Bad message",
            RelatedMessageId = message.MessageId,
        });
        var report = await reportResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var reviewResponse = await moderatorClient.GetAsync($"/api/v1/moderation/reports/{report!.ReportId}/review");
        var review = await reviewResponse.Content.ReadFromJsonAsync<ModerationReportReviewDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        Assert.Equal("sender", review!.SubjectUser!.Username);
        Assert.Equal(senderSession.CurrentUser.UserId, review.SubjectUser.UserId);
        Assert.NotNull(review.RelatedMessage);
        Assert.Contains("message that should resolve", review.RelatedMessage!.Snippet);
    }

    [Fact]
    public async Task UserBanEndpoint_CreatesFullBanAndBlocksRestrictedActions()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();
        using var hostClient = factory.CreateClient();
        using var observerClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        var hostSession = await ApiTestHelpers.RegisterAsync(hostClient, username: "host", email: "host@example.com");
        var observerSession = await ApiTestHelpers.RegisterAsync(observerClient, username: "observer", email: "observer@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);
        ApiTestHelpers.SetBearer(subjectClient, subjectSession.AccessToken);
        ApiTestHelpers.SetBearer(hostClient, hostSession.AccessToken);
        ApiTestHelpers.SetBearer(observerClient, observerSession.AccessToken);

        var reportResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Safety",
            Reason = "Repeated unwanted contact",
            RelatedUserId = subjectSession.CurrentUser.UserId,
        });
        var report = await reportResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var banResponse = await moderatorClient.PostAsJsonAsync("/api/v1/moderation/bans", new CreateUserBanRequest
        {
            SubjectUserId = subjectSession.CurrentUser.UserId,
            Reason = "Full safety ban",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            ReportId = report!.ReportId,
        });
        var ban = await banResponse.Content.ReadFromJsonAsync<UserBanDto>(ApiTestHelpers.JsonOptions);
        var protectedResponse = await subjectClient.GetAsync("/api/v1/profiles/me");
        var loginAfterBanResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "subject",
            Password = "Pa$$w0rd123",
        });
        var refreshAfterBanResponse = await publicClient.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshSessionRequest
        {
            RefreshToken = subjectSession.RefreshToken,
        });

        var createEventResponse = await subjectClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Blocked create",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        var hostCreateEventResponse = await hostClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Open dinner",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var eventDetail = await hostCreateEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        var joinResponse = await subjectClient.PostAsync($"/api/v1/events/{eventDetail!.EventId}/participants", null);
        var discoveryResponse = await observerClient.GetAsync("/api/v1/discovery/people?pageSize=20");
        var discovery = await discoveryResponse.Content.ReadFromJsonAsync<ListResponse<DiscoveryProfilePreviewDto>>(ApiTestHelpers.JsonOptions);
        var chatResponse = await subjectClient.PostAsJsonAsync("/api/v1/support/messages", new SendSupportMessageRequest
        {
            Body = "I should be blocked from sending.",
        });

        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);
        Assert.Equal(
            new[]
            {
                RestrictionScope.DiscoveryVisibility,
                RestrictionScope.ChatSend,
                RestrictionScope.EventJoin,
                RestrictionScope.EventCreate,
            }.OrderBy(scope => scope),
            ban!.Restrictions.Select(restriction => restriction.Scope).OrderBy(scope => scope));
        Assert.Equal(ModerationReportStatus.Resolved, ban.ResolvedReport!.Status);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterBanResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterBanResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, createEventResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, hostCreateEventResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, joinResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
        Assert.DoesNotContain(discovery!.Items, item => item.UserId == subjectSession.CurrentUser.UserId);
        Assert.Equal(HttpStatusCode.Unauthorized, chatResponse.StatusCode);
    }

    [Fact]
    public async Task UserBanEndpoint_WhenCallerIsPlainUser_ReturnsForbidden()
    {
        factory.ResetState();
        using var userClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var userSession = await ApiTestHelpers.RegisterAsync(userClient, username: "user", email: "user@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        ApiTestHelpers.SetBearer(userClient, userSession.AccessToken);

        var response = await userClient.PostAsJsonAsync("/api/v1/moderation/bans", new CreateUserBanRequest
        {
            SubjectUserId = subjectSession.CurrentUser.UserId,
            Reason = "Not authorized",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanUseModeratorEndpointsWithoutModeratorRole()
    {
        factory.ResetState();
        using var adminClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();
        using var bannedClient = factory.CreateClient();

        var adminSession = await ApiTestHelpers.RegisterAsync(adminClient, username: "admin", email: "admin@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        var bannedSession = await ApiTestHelpers.RegisterAsync(bannedClient, username: "banned", email: "banned@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, adminSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });
        ApiTestHelpers.SetBearer(adminClient, adminSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);

        var reportResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Safety",
            Reason = "Needs review",
            RelatedUserId = subjectSession.CurrentUser.UserId,
        });
        var report = await reportResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var listResponse = await adminClient.GetAsync("/api/v1/moderation/reports");
        var getResponse = await adminClient.GetAsync($"/api/v1/moderation/reports/{report!.ReportId}");
        var resolveResponse = await adminClient.PatchAsJsonAsync($"/api/v1/moderation/reports/{report.ReportId}", new ResolveModerationReportRequest
        {
            Decision = "Warned",
            Notes = "Admin reviewed",
        });
        var createRestrictionResponse = await adminClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = subjectSession.CurrentUser.UserId,
            Scope = RestrictionScope.ChatSend,
            Reason = "Cooldown",
        });
        var restriction = await createRestrictionResponse.Content.ReadFromJsonAsync<RestrictionDto>(ApiTestHelpers.JsonOptions);
        var updateRestrictionResponse = await adminClient.PatchAsJsonAsync($"/api/v1/moderation/restrictions/{restriction!.RestrictionId}", new UpdateRestrictionRequest
        {
            Revoke = true,
            Reason = "Reviewed by admin",
        });
        var banResponse = await adminClient.PostAsJsonAsync("/api/v1/moderation/bans", new CreateUserBanRequest
        {
            SubjectUserId = bannedSession.CurrentUser.UserId,
            Reason = "Full safety ban",
        });

        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createRestrictionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateRestrictionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);
    }

    [Fact]
    public async Task ReportQueueEndpoints_SupportSubmitAndResolve()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var reporterClient = factory.CreateClient();
        using var subjectClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var reporterSession = await ApiTestHelpers.RegisterAsync(reporterClient, username: "reporter", email: "reporter@example.com");
        var subjectSession = await ApiTestHelpers.RegisterAsync(subjectClient, username: "subject", email: "subject@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(reporterClient, reporterSession.AccessToken);

        var createResponse = await reporterClient.PostAsJsonAsync("/api/v1/reports", new CreateModerationReportRequest
        {
            TargetType = ReportTargetType.User,
            TargetId = subjectSession.CurrentUser.UserId,
            Category = "Harassment",
            Reason = "Repeated abuse",
        });
        var report = await createResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        var queueResponse = await moderatorClient.GetAsync("/api/v1/moderation/reports");
        var queue = await queueResponse.Content.ReadFromJsonAsync<ListResponse<ModerationReportDto>>(ApiTestHelpers.JsonOptions);

        var resolveResponse = await moderatorClient.PatchAsJsonAsync($"/api/v1/moderation/reports/{report!.ReportId}", new ResolveModerationReportRequest
        {
            Decision = "NoAction",
            Notes = "Reviewed and documented",
        });
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<ModerationReportDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.Contains(queue!.Items, item => item.ReportId == report.ReportId);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        Assert.Equal(ModerationReportStatus.Resolved, resolved!.Status);
    }

    [Fact]
    public async Task RestrictionAndAuditEndpoints_RespectRoleBoundaries()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        using var userClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var adminSession = await ApiTestHelpers.RegisterAsync(adminClient, username: "admin", email: "admin@example.com");
        var userSession = await ApiTestHelpers.RegisterAsync(userClient, username: "user", email: "user@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, adminSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Admin });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(adminClient, adminSession.AccessToken);
        ApiTestHelpers.SetBearer(userClient, userSession.AccessToken);

        var createRestrictionResponse = await moderatorClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = userSession.CurrentUser.UserId,
            Scope = RestrictionScope.EventJoin,
            Reason = "Safety pause",
        });
        var restriction = await createRestrictionResponse.Content.ReadFromJsonAsync<RestrictionDto>(ApiTestHelpers.JsonOptions);

        var userAuditResponse = await userClient.GetAsync("/api/v1/audit-logs");
        var adminAuditResponse = await adminClient.GetAsync("/api/v1/audit-logs");
        var audit = await adminAuditResponse.Content.ReadFromJsonAsync<ListResponse<AuditLogEntryDto>>(ApiTestHelpers.JsonOptions);

        var revokeResponse = await moderatorClient.PatchAsJsonAsync($"/api/v1/moderation/restrictions/{restriction!.RestrictionId}", new UpdateRestrictionRequest
        {
            Revoke = true,
            Reason = "Appeal granted",
        });
        revokeResponse.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, createRestrictionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userAuditResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminAuditResponse.StatusCode);
        Assert.Contains(audit!.Items, item => item.ActionType == "RestrictionCreated");
    }
}
