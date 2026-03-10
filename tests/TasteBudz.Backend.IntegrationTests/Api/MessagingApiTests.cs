// Integration tests for SignalR chat and REST chat history endpoints.
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Verifies live event/group chat plus history retrieval through the real host.
/// </summary>
public sealed class MessagingApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    [Fact]
    public async Task EventChatHub_SupportsJoinSendReceiveAndHistory()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createEventResponse = await ownerClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Event chat",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var eventDetail = await createEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/events/{eventDetail!.EventId}/participants", null);

        await using var ownerConnection = CreateConnection(ownerSession.AccessToken);
        await using var guestConnection = CreateConnection(guestSession.AccessToken);
        var received = new TaskCompletionSource<ChatMessageDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        ownerConnection.On<ChatMessageDto>("MessageReceived", message => received.TrySetResult(message));

        await ownerConnection.StartAsync();
        await guestConnection.StartAsync();
        await ownerConnection.InvokeAsync("JoinScope", ChatScopeType.Event, eventDetail.EventId);
        await guestConnection.InvokeAsync("JoinScope", ChatScopeType.Event, eventDetail.EventId);

        var sent = await guestConnection.InvokeAsync<ChatMessageDto>("SendMessage", new SendChatMessageRequest
        {
            ScopeType = ChatScopeType.Event,
            ScopeId = eventDetail.EventId,
            Body = "See you at seven",
        });
        var receivedMessage = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var historyResponse = await guestClient.GetAsync($"/api/v1/events/{eventDetail.EventId}/messages");
        var history = await historyResponse.Content.ReadFromJsonAsync<CursorPageResponse<ChatMessageDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal("See you at seven", sent.Body);
        Assert.Equal(sent.MessageId, receivedMessage.MessageId);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.Contains(history!.Items, item => item.MessageId == sent.MessageId);
    }

    [Fact]
    public async Task GroupChatHub_SupportsJoinSendReceiveAndHistory()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Group chat",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        await using var ownerConnection = CreateConnection(ownerSession.AccessToken);
        await using var guestConnection = CreateConnection(guestSession.AccessToken);
        var received = new TaskCompletionSource<ChatMessageDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        ownerConnection.On<ChatMessageDto>("MessageReceived", message => received.TrySetResult(message));

        await ownerConnection.StartAsync();
        await guestConnection.StartAsync();
        await ownerConnection.InvokeAsync("JoinScope", ChatScopeType.Group, group.GroupId);
        await guestConnection.InvokeAsync("JoinScope", ChatScopeType.Group, group.GroupId);

        var sent = await guestConnection.InvokeAsync<ChatMessageDto>("SendMessage", new SendChatMessageRequest
        {
            ScopeType = ChatScopeType.Group,
            ScopeId = group.GroupId,
            Body = "Who wants tacos?",
        });
        var receivedMessage = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var historyResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/messages");
        var history = await historyResponse.Content.ReadFromJsonAsync<CursorPageResponse<ChatMessageDto>>(ApiTestHelpers.JsonOptions);

        Assert.Equal("Who wants tacos?", sent.Body);
        Assert.Equal(sent.MessageId, receivedMessage.MessageId);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.Contains(history!.Items, item => item.MessageId == sent.MessageId);
    }

    [Fact]
    public async Task GroupChatHub_SendIsBlockedByChatRestriction()
    {
        factory.ResetState();
        using var moderatorClient = factory.CreateClient();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var moderatorSession = await ApiTestHelpers.RegisterAsync(moderatorClient, username: "mod", email: "mod@example.com");
        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        await ApiTestHelpers.PromoteRolesAsync(factory.Services, moderatorSession.CurrentUser.UserId, new[] { UserRole.User, UserRole.Moderator });
        ApiTestHelpers.SetBearer(moderatorClient, moderatorSession.AccessToken);
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Restricted group",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        await moderatorClient.PostAsJsonAsync("/api/v1/moderation/restrictions", new CreateRestrictionRequest
        {
            SubjectUserId = guestSession.CurrentUser.UserId,
            Scope = RestrictionScope.ChatSend,
            Reason = "Cooldown",
        });

        await using var guestConnection = CreateConnection(guestSession.AccessToken);
        await guestConnection.StartAsync();
        await guestConnection.InvokeAsync("JoinScope", ChatScopeType.Group, group.GroupId);

        await Assert.ThrowsAsync<HubException>(() =>
            guestConnection.InvokeAsync<ChatMessageDto>("SendMessage", new SendChatMessageRequest
            {
                ScopeType = ChatScopeType.Group,
                ScopeId = group.GroupId,
                Body = "This should fail",
            }));
    }

    [Fact]
    public async Task EventMessages_AfterParticipantRemoval_ReturnNotFound()
    {
        factory.ResetState();
        using var hostClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var hostSession = await ApiTestHelpers.RegisterAsync(hostClient, username: "host", email: "host@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(hostClient, hostSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createEventResponse = await hostClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Removal lockout",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var eventDetail = await createEventResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/events/{eventDetail!.EventId}/participants", null);

        var removalResponse = await hostClient.PostAsync($"/api/v1/events/{eventDetail.EventId}/participants/{guestSession.CurrentUser.UserId}/removal", null);
        var historyResponse = await guestClient.GetAsync($"/api/v1/events/{eventDetail.EventId}/messages");

        Assert.Equal(HttpStatusCode.NoContent, removalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, historyResponse.StatusCode);
    }

    [Fact]
    public async Task GroupMessages_AfterMemberLeaves_ReturnNotFound()
    {
        factory.ResetState();
        using var ownerClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var ownerSession = await ApiTestHelpers.RegisterAsync(ownerClient, username: "owner", email: "owner@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(ownerClient, ownerSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createGroupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups", new CreateGroupRequest
        {
            Name = "Leave lockout",
            Visibility = GroupVisibility.Public,
        });
        var group = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>(ApiTestHelpers.JsonOptions);
        await guestClient.PostAsync($"/api/v1/groups/{group!.GroupId}/members", null);

        var leaveResponse = await guestClient.DeleteAsync($"/api/v1/groups/{group.GroupId}/members/me");
        var historyResponse = await guestClient.GetAsync($"/api/v1/groups/{group.GroupId}/messages");

        Assert.Equal(HttpStatusCode.NoContent, leaveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, historyResponse.StatusCode);
    }

    private HubConnection CreateConnection(string accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress!, "/hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
}
