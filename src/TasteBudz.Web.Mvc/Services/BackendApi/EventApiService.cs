using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Payments;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over event browse, detail, participation, and invite endpoints.
/// </summary>
public sealed class EventApiService
{
    private readonly BackendHttpClient backendHttpClient;

    public EventApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<ListResponse<EventSummaryDto>> BrowseAsync(
        BrowseEventsQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<ListResponse<EventSummaryDto>>(
            BuildBrowsePath(query ?? new BrowseEventsQuery()),
            cancellationToken);

    public Task<EventDetailDto> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateEventRequest, EventDetailDto>(
            "/api/v1/events",
            request,
            cancellationToken: cancellationToken);

    public Task<EventDetailDto> GetAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<EventDetailDto>($"/api/v1/events/{eventId}", cancellationToken);

    public Task<EventDetailDto> UpdateAsync(
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateEventRequest, EventDetailDto>(
            $"/api/v1/events/{eventId}",
            request,
            cancellationToken);

    public Task<IReadOnlyCollection<EventParticipantDto>> ListParticipantsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<EventParticipantDto>>(
            $"/api/v1/events/{eventId}/participants",
            cancellationToken);

    public Task<EventParticipantDto> JoinAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<EventParticipantDto>(
            $"/api/v1/events/{eventId}/participants",
            cancellationToken: cancellationToken);

    public Task<EventParticipantDto> UpdateMyParticipationAsync(
        Guid eventId,
        UpdateMyParticipationRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PatchAsync<UpdateMyParticipationRequest, EventParticipantDto>(
            $"/api/v1/events/{eventId}/participants/me",
            request,
            cancellationToken);

    public Task RemoveParticipantAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/events/{eventId}/participants/{userId}/removal",
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<EventParticipantDto>> InviteAsync(
        Guid eventId,
        InviteUsersRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<InviteUsersRequest, IReadOnlyCollection<EventParticipantDto>>(
            $"/api/v1/events/{eventId}/invites",
            request,
            cancellationToken: cancellationToken);

    public Task CancelAsync(
        Guid eventId,
        CancelEventRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync(
            $"/api/v1/events/{eventId}/cancellation",
            request,
            cancellationToken: cancellationToken);

    public Task<EventSlotReservationDto> ReserveSlotAsync(
        Guid eventId,
        ReserveEventSlotRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<ReserveEventSlotRequest, EventSlotReservationDto>(
            $"/api/v1/events/{eventId}/slot-reservations",
            request,
            cancellationToken: cancellationToken);

    public Task<IReadOnlyCollection<EventFeedbackDto>> ListFeedbackAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<IReadOnlyCollection<EventFeedbackDto>>(
            $"/api/v1/events/{eventId}/feedback",
            cancellationToken);

    public Task<EventFeedbackDto> UpsertFeedbackAsync(
        Guid eventId,
        UpsertEventFeedbackRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PutAsync<UpsertEventFeedbackRequest, EventFeedbackDto>(
            $"/api/v1/events/{eventId}/feedback/me",
            request,
            cancellationToken);

    public Task<EventFeedbackPhotoDto> UploadFeedbackPhotoAsync(
        Guid eventId,
        IFormFile file,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostMultipartAsync<EventFeedbackPhotoDto>(
            $"/api/v1/events/{eventId}/feedback/me/photos",
            () => CreateMultipartContent(file),
            cancellationToken: cancellationToken);

    public Task DeleteFeedbackPhotoAsync(
        Guid eventId,
        Guid mediaAssetId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.DeleteAsync(
            $"/api/v1/events/{eventId}/feedback/me/photos/{mediaAssetId}",
            cancellationToken: cancellationToken);

    public Task<BackendFileResponse> GetMediaAsync(Guid mediaAssetId, CancellationToken cancellationToken = default) =>
        backendHttpClient.GetFileAsync($"/api/v1/media/{mediaAssetId}", cancellationToken);

    public Task<CheckoutSessionDto> CreateCheckoutSessionAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CheckoutSessionDto>(
            $"/api/v1/events/{eventId}/checkout-sessions",
            cancellationToken: cancellationToken);

    public Task<CheckoutSessionDto> CompleteCheckoutSessionAsync(
        Guid checkoutSessionId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CheckoutSessionDto>(
            $"/api/v1/checkout-sessions/{checkoutSessionId}/completion",
            cancellationToken: cancellationToken);

    public Task<CheckoutSessionDto> CancelCheckoutSessionAsync(
        Guid checkoutSessionId,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CheckoutSessionDto>(
            $"/api/v1/checkout-sessions/{checkoutSessionId}/cancellation",
            cancellationToken: cancellationToken);

    private static string BuildBrowsePath(BrowseEventsQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            builder.Add("q", query.Q);
        }

        if (!string.IsNullOrWhiteSpace(query.Cuisine))
        {
            builder.Add("cuisine", query.Cuisine);
        }

        if (query.PriceTier.HasValue)
        {
            builder.Add("priceTier", query.PriceTier.Value.ToString());
        }

        if (query.Status.HasValue)
        {
            builder.Add("status", query.Status.Value.ToString());
        }

        if (query.EventType.HasValue)
        {
            builder.Add("eventType", query.EventType.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(query.ZipCode))
        {
            builder.Add("zipCode", query.ZipCode);
        }

        if (query.RadiusMiles.HasValue)
        {
            builder.Add("radiusMiles", query.RadiusMiles.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (query.StartsAfter.HasValue)
        {
            builder.Add("startsAfter", query.StartsAfter.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        if (query.StartsBefore.HasValue)
        {
            builder.Add("startsBefore", query.StartsBefore.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        if (query.AvailabilityOnly)
        {
            builder.Add("availabilityOnly", bool.TrueString.ToLowerInvariant());
        }

        if (query.Recommended)
        {
            builder.Add("recommended", bool.TrueString.ToLowerInvariant());
        }

        if (query.GroupId.HasValue)
        {
            builder.Add("groupId", query.GroupId.Value.ToString());
        }

        if (query.GroupLinked.HasValue)
        {
            builder.Add("groupLinked", query.GroupLinked.Value.ToString().ToLowerInvariant());
        }

        builder.Add("page", query.Page.ToString(CultureInfo.InvariantCulture));
        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"/api/v1/events{builder.ToQueryString()}";
    }

    private static MultipartFormDataContent CreateMultipartContent(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }

        content.Add(fileContent, "file", file.FileName);
        return content;
    }
}
