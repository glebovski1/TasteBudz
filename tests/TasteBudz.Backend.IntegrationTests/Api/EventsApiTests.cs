// Integration tests for the public event HTTP workflow.
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.IntegrationTests.Shared;
using TasteBudz.Backend.Modules.Events;

namespace TasteBudz.Backend.IntegrationTests.Api;

/// <summary>
/// Exercises the main open-event flow across the real HTTP pipeline.
/// </summary>
public sealed class EventsApiTests(TasteBudzApiFactory factory) : IClassFixture<TasteBudzApiFactory>
{
    /// <summary>
    /// Covers the full happy path for an open event from create through cancellation.
    /// </summary>
    [Fact]
    public async Task OpenEventEndpoints_SupportCreateBrowseJoinUpdateAndCancel()
    {
        factory.ResetState();
        using var hostClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();

        var hostSession = await ApiTestHelpers.RegisterAsync(hostClient, username: "host", email: "host@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        ApiTestHelpers.SetBearer(hostClient, hostSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);

        var createResponse = await hostClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Friday Sushi Night",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 3,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);

        var browseResponse = await guestClient.GetAsync("/api/v1/events?cuisine=Sushi&pageSize=10");
        var browse = await browseResponse.Content.ReadFromJsonAsync<ListResponse<EventSummaryDto>>(ApiTestHelpers.JsonOptions);

        var joinResponse = await guestClient.PostAsync($"/api/v1/events/{created!.EventId}/participants", null);
        var joined = await joinResponse.Content.ReadFromJsonAsync<EventParticipantDto>(ApiTestHelpers.JsonOptions);

        var updateResponse = await hostClient.PatchAsJsonAsync($"/api/v1/events/{created.EventId}", new UpdateEventRequest
        {
            Title = "Updated Friday Sushi Night",
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);

        var participantsResponse = await hostClient.GetAsync($"/api/v1/events/{created.EventId}/participants");
        var participants = await participantsResponse.Content.ReadFromJsonAsync<EventParticipantDto[]>(ApiTestHelpers.JsonOptions);

        var cancelResponse = await hostClient.PostAsJsonAsync($"/api/v1/events/{created.EventId}/cancellation", new CancelEventRequest
        {
            Reason = "Restaurant closed",
        });
        var detailAfterCancelResponse = await guestClient.GetAsync($"/api/v1/events/{created.EventId}");
        var detailAfterCancel = await detailAfterCancelResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(1, created.ActiveParticipants);
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.Contains(browse!.Items, item => item.EventId == created.EventId && item.ActiveParticipants == 1);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        Assert.Equal(EventParticipantState.Joined, joined!.State);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Updated Friday Sushi Night", updated!.Title);
        Assert.Equal(HttpStatusCode.OK, participantsResponse.StatusCode);
        Assert.Equal(2, participants!.Length);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailAfterCancelResponse.StatusCode);
        Assert.Equal(EventStatus.Cancelled, detailAfterCancel!.Status);
        Assert.Equal("Restaurant closed", detailAfterCancel.CancellationReason);
    }

    /// <summary>
    /// The HTTP layer should expose the same capacity invariant enforced by the service layer.
    /// </summary>
    [Fact]
    public async Task EventEndpoints_RejectOutOfRangeCapacity()
    {
        factory.ResetState();
        using var client = factory.CreateClient();

        var session = await ApiTestHelpers.RegisterAsync(client, username: "host", email: "host@example.com");
        ApiTestHelpers.SetBearer(client, session.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Bad capacity",
            EventType = EventType.Open,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            Capacity = 1,
            SelectedRestaurantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        });

        var createProblem = await createResponse.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
        Assert.Contains("application/problem+json", createResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, createProblem!.Status);
    }

    [Fact]
    public async Task ClosedEventEndpoints_SupportInvitesResponsesAndHostOnlyGuards()
    {
        factory.ResetState();
        using var hostClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();
        using var samClient = factory.CreateClient();
        using var intruderClient = factory.CreateClient();

        var hostSession = await ApiTestHelpers.RegisterAsync(hostClient, username: "host", email: "host@example.com");
        var guestSession = await ApiTestHelpers.RegisterAsync(guestClient, username: "guest", email: "guest@example.com");
        var samSession = await ApiTestHelpers.RegisterAsync(samClient, username: "sam", email: "sam@example.com");
        var intruderSession = await ApiTestHelpers.RegisterAsync(intruderClient, username: "intruder", email: "intruder@example.com");
        ApiTestHelpers.SetBearer(hostClient, hostSession.AccessToken);
        ApiTestHelpers.SetBearer(guestClient, guestSession.AccessToken);
        ApiTestHelpers.SetBearer(samClient, samSession.AccessToken);
        ApiTestHelpers.SetBearer(intruderClient, intruderSession.AccessToken);

        var createResponse = await hostClient.PostAsJsonAsync("/api/v1/events", new CreateEventRequest
        {
            Title = "Invite only dinner",
            EventType = EventType.Closed,
            EventStartAtUtc = DateTimeOffset.UtcNow.AddDays(2),
            Capacity = 4,
            CuisineTarget = "Thai",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EventDetailDto>(ApiTestHelpers.JsonOptions);

        var inviteResponse = await hostClient.PostAsJsonAsync($"/api/v1/events/{created!.EventId}/invites", new InviteUsersRequest
        {
            Usernames = new[] { "guest", "sam" },
        });
        var invites = await inviteResponse.Content.ReadFromJsonAsync<EventParticipantDto[]>(ApiTestHelpers.JsonOptions);

        var guestJoinResponse = await guestClient.PatchAsJsonAsync($"/api/v1/events/{created.EventId}/participants/me", new UpdateMyParticipationRequest
        {
            State = EventParticipantState.Joined,
        });
        var guestJoined = await guestJoinResponse.Content.ReadFromJsonAsync<EventParticipantDto>(ApiTestHelpers.JsonOptions);

        var samDeclineResponse = await samClient.PatchAsJsonAsync($"/api/v1/events/{created.EventId}/participants/me", new UpdateMyParticipationRequest
        {
            State = EventParticipantState.Declined,
        });
        var samDeclined = await samDeclineResponse.Content.ReadFromJsonAsync<EventParticipantDto>(ApiTestHelpers.JsonOptions);

        var intruderUpdateResponse = await intruderClient.PatchAsJsonAsync($"/api/v1/events/{created.EventId}", new UpdateEventRequest
        {
            Title = "Unauthorized update",
        });
        var intruderInviteResponse = await intruderClient.PostAsJsonAsync($"/api/v1/events/{created.EventId}/invites", new InviteUsersRequest
        {
            Usernames = new[] { "guest" },
        });
        var intruderCancelResponse = await intruderClient.PostAsJsonAsync($"/api/v1/events/{created.EventId}/cancellation", new CancelEventRequest
        {
            Reason = "Unauthorized",
        });
        var intruderRemovalResponse = await intruderClient.PostAsync($"/api/v1/events/{created.EventId}/participants/{guestSession.CurrentUser.UserId}/removal", null);

        var guestLeaveResponse = await guestClient.PatchAsJsonAsync($"/api/v1/events/{created.EventId}/participants/me", new UpdateMyParticipationRequest
        {
            State = EventParticipantState.Left,
        });
        var guestLeft = await guestLeaveResponse.Content.ReadFromJsonAsync<EventParticipantDto>(ApiTestHelpers.JsonOptions);
        var participantsResponse = await hostClient.GetAsync($"/api/v1/events/{created.EventId}/participants");
        var participants = await participantsResponse.Content.ReadFromJsonAsync<EventParticipantDto[]>(ApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, inviteResponse.StatusCode);
        Assert.Equal(2, invites!.Length);
        Assert.Equal(HttpStatusCode.OK, guestJoinResponse.StatusCode);
        Assert.Equal(EventParticipantState.Joined, guestJoined!.State);
        Assert.Equal(HttpStatusCode.OK, samDeclineResponse.StatusCode);
        Assert.Equal(EventParticipantState.Declined, samDeclined!.State);
        Assert.Equal(HttpStatusCode.Forbidden, intruderUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, intruderInviteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, intruderCancelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, intruderRemovalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, guestLeaveResponse.StatusCode);
        Assert.Equal(EventParticipantState.Left, guestLeft!.State);
        Assert.Equal(HttpStatusCode.OK, participantsResponse.StatusCode);
        Assert.Contains(participants!, participant => participant.UserId == samSession.CurrentUser.UserId && participant.State == EventParticipantState.Declined);
        Assert.Contains(participants!, participant => participant.UserId == guestSession.CurrentUser.UserId && participant.State == EventParticipantState.Left);
    }
}
