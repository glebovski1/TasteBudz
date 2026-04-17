using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;

namespace TasteBudz.Web.Mvc.IntegrationTests.Api;

public sealed class MessagingMvcTests
{
    [Fact]
    public async Task EventChat_RendersBackendHubUrlAndAccessTokenForRealtimeConnection()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var eventId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), Guid.NewGuid(), "alex", "Alex Carter", "Hello realtime chat", DateTimeOffset.UtcNow),
                    },
                    null)));

        using var response = await client.GetAsync($"/Messaging/EventChat?eventId={eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("const SCOPE_TYPE = 0;", html);
        Assert.Contains(eventId.ToString(), html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("const HUB_URL = \"https://backend.test/hubs/chat\";", html);
        Assert.Contains("const ACCESS_TOKEN = \"access-token\";", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task DirectChat_RendersDirectScopeAndBackendHubMetadata()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var directChatId = Guid.NewGuid();

        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/direct-chats/{directChatId}/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), Guid.NewGuid(), "sam", "Sam Carter", "Hello direct chat", DateTimeOffset.UtcNow),
                    },
                    null)));

        using var response = await client.GetAsync($"/Messaging/DirectChat?directChatId={directChatId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Direct Chat", html);
        Assert.Contains("const SCOPE_TYPE = 2;", html);
        Assert.Contains(directChatId.ToString(), html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("const HUB_URL = \"https://backend.test/hubs/chat\";", html);
        Assert.Contains("const ACCESS_TOKEN = \"access-token\";", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task SupportChat_RendersSupportScopeForCurrentUser()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var session = await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true);
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            "/api/v1/support/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), session.CurrentUser.UserId, "alex", "Alex Carter", "Need help", DateTimeOffset.UtcNow),
                    },
                    null)));

        using var response = await client.GetAsync("/Messaging/Support");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Support Chat", html);
        Assert.Contains("const SCOPE_TYPE = 3;", html);
        Assert.Contains(session.CurrentUser.UserId.ToString(), html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("const HUB_URL = \"https://backend.test/hubs/chat\";", html);
        Assert.Contains("const ACCESS_TOKEN = \"access-token\";", html);
        factory.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task AdminSupportThread_RendersSupportScopeForSelectedUser()
    {
        using var factory = new TasteBudzMvcFactory();
        using var client = MvcTestHelpers.CreateClient(factory);
        var userId = Guid.NewGuid();
        await MvcTestHelpers.LoginThroughUiAsync(client, factory, isOnboardingComplete: true, roles: new[] { TasteBudz.Backend.Domain.UserRole.User, TasteBudz.Backend.Domain.UserRole.Admin });
        factory.BackendHandler.Requests.Clear();
        factory.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/admin/support/threads/{userId}/messages?pageSize=20",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), userId, "sam", "Sam Carter", "Question", DateTimeOffset.UtcNow),
                    },
                    null)));

        using var response = await client.GetAsync($"/Messaging/AdminSupportThread?userId={userId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Support Chat", html);
        Assert.Contains("const SCOPE_TYPE = 3;", html);
        Assert.Contains(userId.ToString(), html, StringComparison.OrdinalIgnoreCase);
        factory.BackendHandler.AssertDrained();
    }
}
