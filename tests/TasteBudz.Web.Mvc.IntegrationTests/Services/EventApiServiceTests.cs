using System.Net;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Web.Mvc.IntegrationTests.Shared;
using TasteBudz.Web.Mvc.Services;

namespace TasteBudz.Web.Mvc.IntegrationTests.Services;

public sealed class EventApiServiceTests
{
    [Fact]
    public async Task BrowseDetailAndParticipants_SendExpectedRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new EventApiService(client));
        var eventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events?q=sushi&cuisine=Japanese&priceTier=Two&status=Open&eventType=Open&zipCode=45220&radiusMiles=10&availabilityOnly=true&groupId={groupId}&page=2&pageSize=10",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new ListResponse<EventSummaryDto>(
                    new[]
                    {
                        new EventSummaryDto(eventId, "Friday Sushi Night", EventType.Open, EventStatus.Open, new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero), 6, 2, Guid.NewGuid(), null, "Sushi", groupId),
                    },
                    1)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(eventId, "Friday Sushi Night", EventType.Open, EventStatus.Open, new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero), 6, 2, 2, Guid.NewGuid(), null, "Sushi", groupId, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(Guid.NewGuid(), "alex", "Alex Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                }));

        var browse = await service.BrowseAsync(new BrowseEventsQuery
        {
            Q = "sushi",
            Cuisine = "Japanese",
            PriceTier = PriceTier.Two,
            Status = EventStatus.Open,
            EventType = EventType.Open,
            ZipCode = "45220",
            RadiusMiles = 10,
            AvailabilityOnly = true,
            GroupId = groupId,
            Page = 2,
            PageSize = 10,
        });
        var detail = await service.GetAsync(eventId);
        var participants = await service.ListParticipantsAsync(eventId);

        Assert.Single(browse.Items);
        Assert.Equal(eventId, detail.EventId);
        Assert.Single(participants);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task MutationEndpoints_SendExpectedBodiesAndRoutes()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new EventApiService(client));
        var eventId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            "/api/v1/events",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(eventId, "Friday Sushi Night", EventType.Open, EventStatus.Open, new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 20, 17, 0, 0, TimeSpan.Zero), 6, 2, 1, Guid.NewGuid(), restaurantId, null, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/events/{eventId}",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventDetailDto(eventId, "Updated Friday Sushi Night", EventType.Open, EventStatus.Open, new DateTimeOffset(2026, 3, 20, 20, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 20, 18, 0, 0, TimeSpan.Zero), 6, 2, 1, Guid.NewGuid(), restaurantId, null, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/participants",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventParticipantDto(Guid.NewGuid(), "alex", "Alex Carter", EventParticipantState.Joined, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Patch,
            $"/api/v1/events/{eventId}/participants/me",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventParticipantDto(Guid.NewGuid(), "alex", "Alex Carter", EventParticipantState.Left, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/participants/{userId}/removal",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/invites",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventParticipantDto(Guid.NewGuid(), "sam", "Sam Carter", EventParticipantState.Invited, DateTimeOffset.UtcNow, null, null),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/cancellation",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));

        await service.CreateAsync(new CreateEventRequest
        {
            EventType = EventType.Open,
            EventStartAtUtc = new DateTimeOffset(2026, 3, 20, 19, 0, 0, TimeSpan.Zero),
            Capacity = 6,
            Title = "Friday Sushi Night",
            SelectedRestaurantId = restaurantId,
            InviteUsernames = new[] { "sam", "jamie" },
        });
        await service.UpdateAsync(eventId, new UpdateEventRequest
        {
            Title = "Updated Friday Sushi Night",
            EventStartAtUtc = new DateTimeOffset(2026, 3, 20, 20, 0, 0, TimeSpan.Zero),
            Capacity = 6,
        });
        await service.JoinAsync(eventId);
        await service.UpdateMyParticipationAsync(eventId, new UpdateMyParticipationRequest
        {
            State = EventParticipantState.Left,
        });
        await service.RemoveParticipantAsync(eventId, userId);
        await service.InviteAsync(eventId, new InviteUsersRequest
        {
            Usernames = new[] { "sam" },
        });
        await service.CancelAsync(eventId, new CancelEventRequest
        {
            Reason = "Restaurant closed.",
        });

        Assert.Contains(
            "\"inviteUsernames\":[\"sam\",\"jamie\"]",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == "/api/v1/events").Body);
        Assert.Contains(
            "\"title\":\"Updated Friday Sushi Night\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}" && request.Method == HttpMethod.Patch).Body);
        Assert.Contains(
            "\"state\":\"Left\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/participants/me").Body);
        Assert.Contains(
            "\"usernames\":[\"sam\"]",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/invites").Body);
        Assert.Contains(
            "\"reason\":\"Restaurant closed.\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/cancellation").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }
}
