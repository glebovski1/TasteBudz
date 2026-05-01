using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Restaurants;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class AdminMvcTests
{
    [Fact]
    public async Task AdminIndex_RendersOpenPasswordResetRequests()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "I lost access to my old email.",
                Guid.NewGuid(),
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });

        using var response = await client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Password Reset", html);
        Assert.Contains("alex", html);
        Assert.Contains("I lost access to my old email.", html);
        Assert.Contains("Generate Reset Link", html);
        Assert.Contains("Dismiss", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task GeneratePasswordResetToken_FromRequest_PostsRequestIdAndRendersResetLink()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "Please reset my password.",
                userId,
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Admin");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/admin/users/password-reset-tokens",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new PasswordResetTokenDto(
                    userId,
                    "alex",
                    "reset-token",
                    "https://tastebudz.test/Account/ResetPassword?token=reset-token",
                    new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero))));
        EnqueueAdminIndexShell(factory, Array.Empty<PasswordResetRequestDto>());

        using var response = await client.PostAsync(
            "/Admin/GeneratePasswordResetToken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["usernameOrEmail"] = "alex",
                ["passwordResetRequestId"] = requestId.ToString(),
            }));
        var html = await response.Content.ReadAsStringAsync();
        var request = factory.BackendHandler.Requests.Single(recorded =>
            recorded.Method == HttpMethod.Post &&
            recorded.PathAndQuery == "/api/v1/admin/users/password-reset-tokens");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://tastebudz.test/Account/ResetPassword?token=reset-token", html);
        Assert.Contains("\"usernameOrEmail\":\"alex\"", request.Body);
        Assert.Contains($"\"passwordResetRequestId\":\"{requestId}", request.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DismissPasswordResetRequest_PostsClosureAndRedirectsToAdminIndex()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var requestId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        EnqueueAdminIndexShell(factory, new[]
        {
            new PasswordResetRequestDto(
                requestId,
                "alex",
                "Please close this request.",
                Guid.NewGuid(),
                "alex",
                new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                null,
                null),
        });
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, "/Admin");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/users/password-reset-requests/{requestId}/closure",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new PasswordResetRequestDto(
                    requestId,
                    "alex",
                    "Please close this request.",
                    Guid.NewGuid(),
                    "alex",
                    new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 1, 13, 0, 0, TimeSpan.Zero),
                    Guid.NewGuid())));

        using var response = await client.PostAsync(
            "/Admin/DismissPasswordResetRequest",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["requestId"] = requestId.ToString(),
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Admin", response.Headers.Location?.ToString());
        Assert.Equal($"/api/v1/admin/users/password-reset-requests/{requestId}/closure", request.PathAndQuery);
        Assert.Null(request.Body);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task AdminIndex_RendersRestaurantCatalogSummaryWithoutLoadingAssignments()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Admin });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/reports?status=Pending&page=1&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<ModerationReportDto>(Array.Empty<ModerationReportDto>(), 0)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants/search?status=All&page=1&pageSize=1",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<AdminRestaurantCatalogItemDto>(Array.Empty<AdminRestaurantCatalogItemDto>(), 1001)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/users/password-reset-requests",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, Array.Empty<PasswordResetRequestDto>()));

        using var response = await client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("1001 restaurant catalog entries", html);
        Assert.Contains("Open Restaurant Catalog", html);
        Assert.DoesNotContain("admin-assignments", string.Join('\n', factory.BackendHandler.Requests.Select(request => request.PathAndQuery)));
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task Reports_RendersDetailLinksForEachReport()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var reportId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Moderator });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/reports?page=1&pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<ModerationReportDto>(
                    new[]
                    {
                        CreateReport(reportId),
                    },
                    1)));

        using var response = await client.GetAsync("/Admin/Reports");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"/Admin/Reports/{reportId}", html);
        Assert.Contains("View Details", html);
        factory.BackendHandler.AssertDrained();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Moderator)]
    public async Task Search_RendersModerationSearchResults(UserRole role)
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var messageId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var subject = CreateUser(subjectUserId, "subject", "Subject Person");

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, role });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/search?q=needle&type=Message&page=1&pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationSearchResponseDto(
                    "needle",
                    ModerationSearchResultKind.Message,
                    new[]
                    {
                        new ModerationSearchResultDto(
                            ModerationSearchResultKind.Message,
                            messageId,
                            "Message from Subject Person (@subject)",
                            "Support chat",
                            "needle moderation message",
                            new DateTimeOffset(2026, 5, 1, 13, 0, 0, TimeSpan.Zero),
                            subject,
                            null,
                            "Support",
                            subjectUserId),
                    },
                    1)));

        using var response = await client.GetAsync("/Admin/Search?q=needle&type=Message");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Content Search", html);
        Assert.Contains("Message from Subject Person (@subject)", html);
        Assert.Contains("needle moderation message", html);
        Assert.Contains($"/Admin/Users/{subjectUserId}", html);
        factory.BackendHandler.AssertDrained();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Moderator)]
    public async Task Search_WhenQueryBlankAndTypeSelected_RendersBrowsableContent(UserRole role)
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var subjectUserId = Guid.NewGuid();
        var subject = CreateUser(subjectUserId, "subject", "Subject Person");

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, role });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/search?type=User&page=1&pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ModerationSearchResponseDto(
                    null,
                    ModerationSearchResultKind.User,
                    new[]
                    {
                        new ModerationSearchResultDto(
                            ModerationSearchResultKind.User,
                            subjectUserId,
                            "Subject Person (@subject)",
                            "Active account - User",
                            "subject@example.com",
                            null,
                            subject,
                            null,
                            "User",
                            subjectUserId),
                    },
                    1)));

        using var response = await client.GetAsync("/Admin/Search?type=User");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Subject Person (@subject)", html);
        Assert.Contains($"/Admin/Users/{subjectUserId}", html);
        Assert.DoesNotContain("Enter a search term", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ReportDetail_RendersModerationReport()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var reportId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Moderator });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/reports/{reportId}/review",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                CreateReportReview(reportId, subjectUserId)));

        using var response = await client.GetAsync($"/Admin/Reports/{reportId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Report Detail", html);
        Assert.Contains("Repeated unwanted contact", html);
        Assert.Contains("DiscoveryVisibility", html);
        Assert.Contains("Subject Person (@subject)", html);
        Assert.Contains($"/Admin/Users/{subjectUserId}", html);
        Assert.Contains("Ban User", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task ReportDetail_WhenUserTargetHasNoRelatedUser_RendersBanFormForTarget()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var reportId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, UserRole.Moderator });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/reports/{reportId}/review",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                CreateReportReview(reportId, subjectUserId, targetUserId: subjectUserId)));

        using var response = await client.GetAsync($"/Admin/Reports/{reportId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ban User", html);
        Assert.Contains($"name=\"userId\" value=\"{subjectUserId}\"", html);
        Assert.Contains($"name=\"reportId\" value=\"{reportId}\"", html);
        factory.BackendHandler.AssertDrained();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Moderator)]
    public async Task BanUser_FromReport_PostsFullBanPayload(UserRole role)
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var reportId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(
            client,
            factory,
            isOnboardingComplete: true,
            roles: new[] { UserRole.User, role });
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/moderation/reports/{reportId}/review",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                CreateReportReview(reportId, subjectUserId)));
        var token = await MvcTestHelpers.GetRequestVerificationTokenAsync(client, $"/Admin/Reports/{reportId}");
        factory.BackendHandler.AssertDrained();
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/moderation/bans",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new UserBanDto(
                    subjectUserId,
                    new[]
                    {
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.DiscoveryVisibility, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.ChatSend, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.EventJoin, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                        new RestrictionDto(Guid.NewGuid(), subjectUserId, RestrictionScope.EventCreate, "Full safety ban", DateTimeOffset.UtcNow, null, RestrictionStatus.Active, null),
                    },
                    (CreateReport(reportId, relatedUserId: subjectUserId) with
                    {
                        Status = ModerationReportStatus.Resolved,
                        ResolutionDecision = "UserSoftBanned"
                    }))));

        using var response = await client.PostAsync(
            "/Admin/BanUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["userId"] = subjectUserId.ToString(),
                ["reportId"] = reportId.ToString(),
                ["reason"] = "Full safety ban",
                ["permanent"] = "true",
            }));

        var request = Assert.Single(factory.BackendHandler.Requests);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Admin", response.Headers.Location?.ToString());
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/moderation/bans", request.PathAndQuery);
        Assert.Contains($"\"subjectUserId\":\"{subjectUserId}", request.Body);
        Assert.Contains($"\"reportId\":\"{reportId}", request.Body);
        Assert.Contains("\"reason\":\"Full safety ban\"", request.Body);
        factory.BackendHandler.AssertDrained();
    }

    private static void EnqueueAdminIndexShell(
        TasteBudzMvcFactory factory,
        IReadOnlyCollection<PasswordResetRequestDto> openRequests)
    {
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/moderation/reports?status=Pending&page=1&pageSize=50",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<ModerationReportDto>(Array.Empty<ModerationReportDto>(), 0)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/restaurants/search?status=All&page=1&pageSize=1",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<AdminRestaurantCatalogItemDto>(Array.Empty<AdminRestaurantCatalogItemDto>(), 0)));
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/users/password-reset-requests",
            (_, _) => StubBackendApiHandler.Json(HttpStatusCode.OK, openRequests));
    }

    private static ModerationReportDto CreateReport(Guid reportId, Guid? relatedUserId = null, Guid? targetUserId = null) =>
        new(
            reportId,
            Guid.NewGuid(),
            ReportTargetType.User,
            targetUserId ?? relatedUserId ?? Guid.NewGuid(),
            "Safety",
            "Repeated unwanted contact",
            "DiscoveryVisibility restriction requested.",
            Guid.NewGuid(),
            relatedUserId,
            null,
            new DateTimeOffset(2026, 5, 1, 13, 0, 0, TimeSpan.Zero),
            ModerationReportStatus.Pending,
            null,
            null,
            null,
            null);

    private static ModerationReportReviewDto CreateReportReview(Guid reportId, Guid subjectUserId, Guid? targetUserId = null)
    {
        var reporterUserId = Guid.NewGuid();
        var reporter = CreateUser(reporterUserId, "reporter", "Reporter Person");
        var subject = CreateUser(subjectUserId, "subject", "Subject Person");

        return new ModerationReportReviewDto(
            new ModerationReportDto(
                reportId,
                reporterUserId,
                ReportTargetType.User,
                targetUserId ?? subjectUserId,
                "Safety",
                "Repeated unwanted contact",
                "DiscoveryVisibility restriction requested.",
                Guid.NewGuid(),
                targetUserId.HasValue ? null : subjectUserId,
                null,
                new DateTimeOffset(2026, 5, 1, 13, 0, 0, TimeSpan.Zero),
                ModerationReportStatus.Pending,
                null,
                null,
                null,
                null),
            reporter,
            subject,
            targetUserId.HasValue ? null : subject,
            null);
    }

    private static ModerationUserSummaryDto CreateUser(Guid userId, string username, string displayName) =>
        new(
            userId,
            username,
            displayName,
            $"{username}@example.com",
            AccountStatus.Active,
            new[] { UserRole.User });
}
