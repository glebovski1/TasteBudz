using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class MessagingApiServiceTests
{
    [Fact]
    public async Task MessageHistoryEndpoints_SendExpectedRoutesAndExposeHubMetadata()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new MessagingApiService(client));
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/messages?cursor=cursor-1&pageSize=30",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), Guid.NewGuid(), "alex", "Alex Carter", "Hello event chat", DateTimeOffset.UtcNow),
                    },
                    "cursor-2")));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/groups/{groupId}/messages?pageSize=15",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), Guid.NewGuid(), "sam", "Sam Carter", "Hello group chat", DateTimeOffset.UtcNow),
                    },
                    null)));

        var eventMessages = await service.ListEventMessagesAsync(eventId, new ChatHistoryQuery
        {
            Cursor = "cursor-1",
            PageSize = 30,
        });
        var groupMessages = await service.ListGroupMessagesAsync(groupId, new ChatHistoryQuery
        {
            PageSize = 15,
        });

        Assert.Equal("/hubs/chat", MessagingApiService.HubPath);
        Assert.Equal("JoinScope", MessagingApiService.JoinScopeMethodName);
        Assert.Equal("SendMessage", MessagingApiService.SendMessageMethodName);
        Assert.Equal("MessageReceived", MessagingApiService.MessageReceivedEventName);
        Assert.Single(eventMessages.Items);
        Assert.Equal("cursor-2", eventMessages.NextCursor);
        Assert.Single(groupMessages.Items);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
