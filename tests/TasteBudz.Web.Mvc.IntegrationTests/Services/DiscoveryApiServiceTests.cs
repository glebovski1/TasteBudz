using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class DiscoveryApiServiceTests
{
    [Fact]
    public async Task DiscoveryEndpoints_SendExpectedRoutesAndPayloads()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new DiscoveryApiService(client));
        var subjectUserId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/discovery/people?q=alex&page=2&pageSize=11",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<DiscoveryProfilePreviewDto>(
                    new[]
                    {
                        new DiscoveryProfilePreviewDto(subjectUserId, "alex", "Alex Carter", "Always down for noodles.", SocialGoal.Friends, new[] { "Ramen" }, new[] { "Vegetarian" }),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/discovery/swipe-candidates?page=3&pageSize=7",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<DiscoveryProfilePreviewDto>(
                    new[]
                    {
                        new DiscoveryProfilePreviewDto(subjectUserId, "sam", "Sam Carter", "Sushi fan.", SocialGoal.Networking, new[] { "Sushi" }, Array.Empty<string>()),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/discovery/swipes",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new SwipeDecisionResultDto(subjectUserId, SwipeDecisionType.Like, true, Guid.NewGuid())));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/budz",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new BudConnectionDto(subjectUserId, "sam", "Sam Carter", DateTimeOffset.UtcNow),
                }));

        var people = await service.SearchPeopleAsync(new SearchPeopleQuery
        {
            Q = "alex",
            Page = 2,
            PageSize = 11,
        });
        var swipeCandidates = await service.GetSwipeCandidatesAsync(new SwipeCandidatesQuery
        {
            Page = 3,
            PageSize = 7,
        });
        var swipeResult = await service.RecordSwipeAsync(new RecordSwipeDecisionRequest
        {
            SubjectUserId = subjectUserId,
            Decision = SwipeDecisionType.Like,
        });
        var budz = await service.ListBudzAsync();

        Assert.Single(people.Items);
        Assert.Single(swipeCandidates.Items);
        Assert.True(swipeResult.IsBudMatch);
        Assert.Single(budz);
        Assert.Contains(
            "\"decision\":\"Like\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/discovery/swipes").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
