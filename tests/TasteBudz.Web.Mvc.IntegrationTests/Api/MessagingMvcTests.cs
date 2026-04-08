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
            $"/api/v1/events/{eventId}/messages?pageSize=15",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CursorPageResponse<ChatMessageDto>(
                    new[]
                    {
                        new ChatMessageDto(Guid.NewGuid(), Guid.NewGuid(), "alex", "Alex Carter", "Hello realtime chat", DateTimeOffset.UtcNow),
                    },
                    null)));

        using var response = await client.GetAsync($"/Messaging/EventChat/{eventId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("const SCOPE_TYPE = 0;", html);
        Assert.Contains("const HUB_URL = \"https://backend.test/hubs/chat\";", html);
        Assert.Contains("const ACCESS_TOKEN = \"access-token\";", html);
        factory.BackendHandler.AssertDrained();
    }
}
