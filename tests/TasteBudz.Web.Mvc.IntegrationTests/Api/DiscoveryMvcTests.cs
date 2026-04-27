using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class DiscoveryMvcTests
{
    [Fact]
    public async Task SwipePage_RendersRetrySafeFailureMessageForSwipePosts()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var subjectUserId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();

        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/discovery/swipe-candidates?page=1&pageSize=10",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<DiscoveryProfilePreviewDto>(
                    new[]
                    {
                        new DiscoveryProfilePreviewDto(
                            subjectUserId,
                            "sam",
                            "Sam Carter",
                            "Always checking out ramen spots.",
                            SocialGoal.Friends,
                            new[] { "Ramen" },
                            Array.Empty<string>()),
                    },
                    1)));

        using var response = await client.GetAsync("/Discovery/Swipe");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("We could not save that swipe. Please try again.", html);
        Assert.Contains("isSwipeInFlight", html);
        Assert.Contains(subjectUserId.ToString(), html);
        factory.BackendHandler.AssertDrained();
    }
}
