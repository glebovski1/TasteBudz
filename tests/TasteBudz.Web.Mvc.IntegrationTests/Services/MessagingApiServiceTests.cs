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
        var directChatId = Guid.NewGuid();
        var subjectUserId = Guid.NewGuid();
        var supportUserId = Guid.NewGuid();

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
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/direct-chats",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new DirectChatDto(directChatId, subjectUserId, "sam", "Sam Carter", DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/direct-chats/{directChatId}/messages?pageSize=10",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), subjectUserId, "sam", "Sam Carter", "Hello direct chat", DateTimeOffset.UtcNow),
                    },
                    null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/direct-chats/{directChatId}/messages",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ChatMessageDto(Guid.NewGuid(), subjectUserId, "alex", "Alex Carter", "Direct reply", DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/support/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), subjectUserId, "alex", "Alex Carter", "Support question", DateTimeOffset.UtcNow),
                    },
                    null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/support/messages",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ChatMessageDto(Guid.NewGuid(), subjectUserId, "alex", "Alex Carter", "Support follow-up", DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/admin/support/threads",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new SupportThreadDto(supportUserId, "alex", "Alex Carter", DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow, "Support question", 1),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/support/threads/{supportUserId}/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), subjectUserId, "alex", "Alex Carter", "Support question", DateTimeOffset.UtcNow),
                    },
                    null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/admin/support/threads/{supportUserId}/messages",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ChatMessageDto(Guid.NewGuid(), subjectUserId, "admin", "Admin", "Support reply", DateTimeOffset.UtcNow)));

        var eventMessages = await service.ListEventMessagesAsync(eventId, new ChatHistoryQuery
        {
            Cursor = "cursor-1",
            PageSize = 30,
        });
        var groupMessages = await service.ListGroupMessagesAsync(groupId, new ChatHistoryQuery
        {
            PageSize = 15,
        });
        var directChat = await service.CreateDirectChatAsync(new CreateDirectChatRequest
        {
            SubjectUserId = subjectUserId,
        });
        var directMessages = await service.ListDirectMessagesAsync(directChatId, new ChatHistoryQuery
        {
            PageSize = 10,
        });
        var directMessage = await service.SendDirectMessageAsync(directChatId, new SendDirectChatMessageRequest
        {
            Body = "Direct reply",
        });
        var supportMessages = await service.ListSupportMessagesAsync();
        var supportMessage = await service.SendSupportMessageAsync(new SendSupportMessageRequest
        {
            Body = "Support follow-up",
        });
        var supportThreads = await service.ListSupportThreadsAsync();
        var adminSupportMessages = await service.ListAdminSupportMessagesAsync(supportUserId);
        var adminSupportMessage = await service.SendAdminSupportMessageAsync(supportUserId, new SendSupportMessageRequest
        {
            Body = "Support reply",
        });

        Assert.Equal("/hubs/chat", MessagingApiService.HubPath);
        Assert.Equal("JoinScope", MessagingApiService.JoinScopeMethodName);
        Assert.Equal("SendMessage", MessagingApiService.SendMessageMethodName);
        Assert.Equal("MessageReceived", MessagingApiService.MessageReceivedEventName);
        Assert.Single(eventMessages.Items);
        Assert.Equal("cursor-2", eventMessages.NextCursor);
        Assert.Single(groupMessages.Items);
        Assert.Equal(directChatId, directChat.DirectChatId);
        Assert.Single(directMessages.Items);
        Assert.Equal("Direct reply", directMessage.Body);
        Assert.Single(supportMessages.Items);
        Assert.Equal("Support follow-up", supportMessage.Body);
        Assert.Single(supportThreads);
        Assert.Single(adminSupportMessages.Items);
        Assert.Equal("Support reply", adminSupportMessage.Body);
        Assert.Contains(
            "\"subjectUserId\":\"" + subjectUserId,
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/direct-chats").Body);
        Assert.Contains(
            "\"body\":\"Direct reply\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/direct-chats/{directChatId}/messages").Body);
        Assert.Contains(
            "\"body\":\"Support follow-up\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/support/messages").Body);
        Assert.Contains(
            "\"body\":\"Support reply\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/admin/support/threads/{supportUserId}/messages").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
