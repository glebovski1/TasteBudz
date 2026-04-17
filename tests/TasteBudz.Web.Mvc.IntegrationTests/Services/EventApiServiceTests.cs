using System.Net;
using Microsoft.AspNetCore.Http;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Payments;
using TasteBudz.Backend.Modules.Restaurants;
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
        var checkoutSessionId = Guid.NewGuid();

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
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/slot-reservations",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventSlotReservationDto(Guid.NewGuid(), eventId, restaurantId, restaurantId, EventSlotReservationStatus.Active, DateTimeOffset.UtcNow, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/checkout-sessions",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CheckoutSessionDto(checkoutSessionId, eventId, userId, CheckoutSessionStatus.Pending, "USD", 2500, 0, 2500, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/checkout-sessions/{checkoutSessionId}/completion",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CheckoutSessionDto(checkoutSessionId, eventId, userId, CheckoutSessionStatus.Completed, "USD", 2500, 0, 2500, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/checkout-sessions/{checkoutSessionId}/cancellation",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new CheckoutSessionDto(checkoutSessionId, eventId, userId, CheckoutSessionStatus.Cancelled, "USD", 2500, 0, 2500, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow)));

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
        await service.ReserveSlotAsync(eventId, new ReserveEventSlotRequest
        {
            SlotId = restaurantId,
        });
        var checkout = await service.CreateCheckoutSessionAsync(eventId);
        var completedCheckout = await service.CompleteCheckoutSessionAsync(checkoutSessionId);
        var cancelledCheckout = await service.CancelCheckoutSessionAsync(checkoutSessionId);

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
        Assert.Contains(
            "\"slotId\":\"" + restaurantId,
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/slot-reservations").Body);
        Assert.Equal(CheckoutSessionStatus.Pending, checkout.Status);
        Assert.Equal(CheckoutSessionStatus.Completed, completedCheckout.Status);
        Assert.Equal(CheckoutSessionStatus.Cancelled, cancelledCheckout.Status);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    [Fact]
    public async Task FeedbackEndpoints_SendExpectedRoutesAndMultipartUpload()
    {
        var context = new BackendApiServiceTestContext();
        await context.SignInAsync();
        var service = context.CreateService(client => new EventApiService(client));
        var eventId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow;

        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/events/{eventId}/feedback",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new[]
                {
                    new EventFeedbackDto(feedbackId, eventId, authorId, "alex", "Alex Carter", 5, "Great night.", Array.Empty<EventFeedbackPhotoDto>(), updatedAt, updatedAt),
                }));
        context.BackendHandler.Enqueue(
            HttpMethod.Put,
            $"/api/v1/events/{eventId}/feedback/me",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventFeedbackDto(feedbackId, eventId, authorId, "alex", "Alex Carter", 4, "Updated feedback.", Array.Empty<EventFeedbackPhotoDto>(), updatedAt, updatedAt)));
        context.BackendHandler.Enqueue(
            HttpMethod.Post,
            $"/api/v1/events/{eventId}/feedback/me/photos",
            (_, _) => StubBackendApiHandler.Json(
                HttpStatusCode.OK,
                new EventFeedbackPhotoDto(mediaAssetId, "feedback.png", "image/png", 3, updatedAt)));
        context.BackendHandler.Enqueue(
            HttpMethod.Delete,
            $"/api/v1/events/{eventId}/feedback/me/photos/{mediaAssetId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.NoContent));
        context.BackendHandler.Enqueue(
            HttpMethod.Get,
            $"/api/v1/media/{mediaAssetId}",
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") },
                },
            });

        var list = await service.ListFeedbackAsync(eventId);
        var updated = await service.UpsertFeedbackAsync(eventId, new UpsertEventFeedbackRequest
        {
            Rating = 4,
            Text = "Updated feedback.",
        });
        var uploaded = await service.UploadFeedbackPhotoAsync(eventId, CreateFormFile("feedback.png", "image/png", new byte[] { 1, 2, 3 }));
        await service.DeleteFeedbackPhotoAsync(eventId, mediaAssetId);
        var media = await service.GetMediaAsync(mediaAssetId);

        Assert.Single(list);
        Assert.Equal(4, updated.Rating);
        Assert.Equal(mediaAssetId, uploaded.MediaAssetId);
        Assert.Equal("image/png", media.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, media.Content);
        Assert.Contains(
            "\"text\":\"Updated feedback.\"",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/feedback/me").Body);
        Assert.Contains(
            "feedback.png",
            context.BackendHandler.Requests.Single(request => request.PathAndQuery == $"/api/v1/events/{eventId}/feedback/me/photos").Body);
        Assert.All(context.BackendHandler.Requests, request => Assert.Equal("access-token", request.AuthorizationParameter));
        context.BackendHandler.AssertDrained();
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
