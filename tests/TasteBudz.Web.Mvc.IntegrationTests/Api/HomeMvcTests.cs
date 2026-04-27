using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class HomeMvcTests
{
    [Fact]
    public async Task HomePage_AdvertisesDefaultEnabledChatScopes()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("event and group chats", html);
        Assert.Contains("support", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("one-on-one", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticatedLayout_NotificationBellReportsMarkReadFailures()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/discovery/swipe-candidates?page=1&pageSize=10",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<DiscoveryProfilePreviewDto>(Array.Empty<DiscoveryProfilePreviewDto>(), 0)));

        using var response = await client.GetAsync("/Discovery/Swipe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Could not mark this notification as read.", html);
        Assert.DoesNotContain("catch { /* silent */ }", html);
        factory.BackendHandler.AssertDrained();
    }
}
