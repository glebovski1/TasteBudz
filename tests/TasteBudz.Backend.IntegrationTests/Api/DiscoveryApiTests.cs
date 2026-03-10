// Integration tests for discovery and Budz HTTP workflows.
using System.Net;
using System.Net.Http.Json;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Exercises discovery search, swipes, Budz, and related notification behavior.
/// </summary>
public sealed class DiscoveryApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task DiscoveryEndpoints_SupportSearchSwipeAndBudzList()
    {
        factory.ResetState();
        using var alexClient = factory.CreateClient();
        using var samClient = factory.CreateClient();

        var alexSession = await ApiTestHelpers.RegisterAsync(alexClient, username: "alex", email: "alex@example.com");
        var samSession = await ApiTestHelpers.RegisterAsync(samClient, username: "sam", email: "sam@example.com");
        ApiTestHelpers.SetBearer(alexClient, alexSession.AccessToken);
        ApiTestHelpers.SetBearer(samClient, samSession.AccessToken);

        var searchResponse = await alexClient.GetAsync("/api/v1/discovery/people?q=sam&pageSize=10");
        var search = await searchResponse.Content.ReadFromJsonAsync<ListResponse<DiscoveryProfilePreviewDto>>(ApiTestHelpers.JsonOptions);

        var firstSwipeResponse = await alexClient.PostAsJsonAsync("/api/v1/discovery/swipes", new RecordSwipeDecisionRequest
        {
            SubjectUserId = samSession.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });
        firstSwipeResponse.EnsureSuccessStatusCode();

        var secondSwipeResponse = await samClient.PostAsJsonAsync("/api/v1/discovery/swipes", new RecordSwipeDecisionRequest
        {
            SubjectUserId = alexSession.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });
        var secondSwipe = await secondSwipeResponse.Content.ReadFromJsonAsync<SwipeDecisionResultDto>(ApiTestHelpers.JsonOptions);

        var budzResponse = await alexClient.GetAsync("/api/v1/budz");
        var budz = await budzResponse.Content.ReadFromJsonAsync<BudConnectionDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Contains(search!.Items, item => item.UserId == samSession.CurrentUser.UserId);
        Assert.Equal(HttpStatusCode.OK, secondSwipeResponse.StatusCode);
        Assert.True(secondSwipe!.IsBudMatch);
        Assert.Equal(HttpStatusCode.OK, budzResponse.StatusCode);
        Assert.Contains(budz!, item => item.UserId == samSession.CurrentUser.UserId);
    }

    [Fact]
    public async Task DiscoverySearch_RespectsPrivacyBlockAndRestrictionFilters()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var callerClient = factory.CreateClient();
        using var visibleClient = factory.CreateClient();
        using var hiddenClient = factory.CreateClient();
        using var blockedClient = factory.CreateClient();
        using var restrictedClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var callerSession = await ApiTestHelpers.RegisterAsync(callerClient, username: "caller", email: "caller@example.com");
        var visibleSession = await ApiTestHelpers.RegisterAsync(visibleClient, username: "visible", email: "visible@example.com");
        var hiddenSession = await ApiTestHelpers.RegisterAsync(hiddenClient, username: "hidden", email: "hidden@example.com");
        var blockedSession = await ApiTestHelpers.RegisterAsync(blockedClient, username: "blocked", email: "blocked@example.com");
        var restrictedSession = await ApiTestHelpers.RegisterAsync(restrictedClient, username: "restricted", email: "restricted@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(callerClient, callerSession.AccessToken);
        ApiTestHelpers.SetBearer(hiddenClient, hiddenSession.AccessToken);

        await hiddenClient.PatchAsJsonAsync("/api/v1/privacy-settings/me", new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });

        await callerClient.PostAsJsonAsync("/api/v1/blocks", new CreateBlockRequest
        {
            BlockedUserId = blockedSession.CurrentUser.UserId,
        });

        await moderatorClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = restrictedSession.CurrentUser.UserId,
            Scope = RestrictionScope.DiscoveryVisibility,
            Reason = "Hidden from discovery",
        });

        var response = await callerClient.GetAsync("/api/v1/discovery/people?pageSize=10");
        var result = await response.Content.ReadFromJsonAsync<ListResponse<DiscoveryProfilePreviewDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(result!.Items, item => item.UserId == visibleSession.CurrentUser.UserId);
        Assert.DoesNotContain(result.Items, item => item.UserId == hiddenSession.CurrentUser.UserId);
        Assert.DoesNotContain(result.Items, item => item.UserId == blockedSession.CurrentUser.UserId);
        Assert.DoesNotContain(result.Items, item => item.UserId == restrictedSession.CurrentUser.UserId);
    }

    [Fact]
    public async Task SwipeCandidates_ExcludeConnectedAndFilteredUsersWhileKeepingTotalCount()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var callerClient = factory.CreateClient();
        using var alphaClient = factory.CreateClient();
        using var betaClient = factory.CreateClient();
        using var hiddenClient = factory.CreateClient();
        using var blockedClient = factory.CreateClient();
        using var restrictedClient = factory.CreateClient();
        using var budClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var callerSession = await ApiTestHelpers.RegisterAsync(callerClient, username: "caller", email: "caller@example.com");
        var alphaSession = await ApiTestHelpers.RegisterAsync(alphaClient, username: "alpha", email: "alpha@example.com");
        var betaSession = await ApiTestHelpers.RegisterAsync(betaClient, username: "beta", email: "beta@example.com");
        var hiddenSession = await ApiTestHelpers.RegisterAsync(hiddenClient, username: "hidden", email: "hidden@example.com");
        var blockedSession = await ApiTestHelpers.RegisterAsync(blockedClient, username: "blocked", email: "blocked@example.com");
        var restrictedSession = await ApiTestHelpers.RegisterAsync(restrictedClient, username: "restricted", email: "restricted@example.com");
        var budSession = await ApiTestHelpers.RegisterAsync(budClient, username: "bud", email: "bud@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(callerClient, callerSession.AccessToken);
        ApiTestHelpers.SetBearer(hiddenClient, hiddenSession.AccessToken);
        ApiTestHelpers.SetBearer(budClient, budSession.AccessToken);

        await moderatorClient.PatchAsJsonAsync("/api/v1/privacy-settings/me", new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });

        await hiddenClient.PatchAsJsonAsync("/api/v1/privacy-settings/me", new UpdatePrivacySettingsRequest
        {
            DiscoveryEnabled = false,
        });

        await callerClient.PostAsJsonAsync("/api/v1/blocks", new CreateBlockRequest
        {
            BlockedUserId = blockedSession.CurrentUser.UserId,
        });

        await moderatorClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = restrictedSession.CurrentUser.UserId,
            Scope = RestrictionScope.DiscoveryVisibility,
            Reason = "Hidden from discovery",
        });

        await callerClient.PostAsJsonAsync("/api/v1/discovery/swipes", new RecordSwipeDecisionRequest
        {
            SubjectUserId = budSession.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });
        await budClient.PostAsJsonAsync("/api/v1/discovery/swipes", new RecordSwipeDecisionRequest
        {
            SubjectUserId = callerSession.CurrentUser.UserId,
            Decision = SwipeDecisionType.Like,
        });

        var response = await callerClient.GetAsync("/api/v1/discovery/swipe-candidates?page=1&pageSize=1");
        var result = await response.Content.ReadFromJsonAsync<ListResponse<DiscoveryProfilePreviewDto>>(ApiTestHelpers.JsonOptions);
        var candidate = Assert.Single(result!.Items);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(alphaSession.CurrentUser.UserId, candidate.UserId);
        Assert.Equal(2, result.TotalCount);
    }
}
