// Browse logic for open events, including search, availability, distance, and lifecycle filtering.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Builds the open-event browse response from event, profile, and restaurant data.
/// </summary>
public sealed class EventBrowseService(
    IEventRepository eventRepository,
    IRestaurantRepository restaurantRepository,
    IProfileRepository profileRepository,
    EventLifecycleService lifecycleService)
{
    public async Task<ListResponse<EventSummaryDto>> BrowseAsync(Guid currentUserId, BrowseEventsQuery query, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.ListAsync(cancellationToken);
        var synchronized = new List<Event>(events.Count);

        foreach (var eventRecord in events)
        {
            synchronized.Add(await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken));
        }

        var restaurants = (await restaurantRepository.ListAsync(cancellationToken)).ToDictionary(restaurant => restaurant.Id);
        var currentProfile = await profileRepository.GetProfileAsync(currentUserId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");
        var referencePoint = string.IsNullOrWhiteSpace(query.ZipCode)
            ? null
            : await restaurantRepository.GetZipCoordinatesAsync(query.ZipCode.Trim(), cancellationToken);
        var recurringAvailability = query.AvailabilityOnly
            ? await profileRepository.ListRecurringAvailabilityAsync(currentUserId, cancellationToken)
            : Array.Empty<RecurringAvailabilityWindow>();
        var oneOffAvailability = query.AvailabilityOnly
            ? await profileRepository.ListOneOffAvailabilityAsync(currentUserId, cancellationToken)
            : Array.Empty<OneOffAvailabilityWindow>();

        var filtered = new List<Event>();

        foreach (var eventRecord in synchronized)
        {
            // The current MVP browse surface only exposes open events.
            if (eventRecord.EventType != EventType.Open)
            {
                continue;
            }

            if (query.EventType.HasValue && query.EventType.Value != EventType.Open)
            {
                continue;
            }

            if (query.Status.HasValue && eventRecord.Status != query.Status.Value)
            {
                continue;
            }

            if (query.StartsAfter.HasValue && eventRecord.EventStartAtUtc < query.StartsAfter.Value)
            {
                continue;
            }

            if (query.StartsBefore.HasValue && eventRecord.EventStartAtUtc > query.StartsBefore.Value)
            {
                continue;
            }

            if (query.GroupId.HasValue && eventRecord.GroupId != query.GroupId.Value)
            {
                continue;
            }

            if (!MatchesQuery(eventRecord, query.Q))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.Cuisine) && !MatchesCuisine(eventRecord, query.Cuisine.Trim(), restaurants))
            {
                continue;
            }

            if (query.PriceTier.HasValue && !MatchesPriceTier(eventRecord, query.PriceTier.Value, restaurants))
            {
                continue;
            }

            if (query.AvailabilityOnly && !MatchesAvailability(eventRecord.EventStartAtUtc, recurringAvailability, oneOffAvailability))
            {
                continue;
            }

            if (query.RadiusMiles.HasValue && !await MatchesDistanceAsync(eventRecord, restaurants, referencePoint, currentProfile, query.RadiusMiles.Value, cancellationToken))
            {
                continue;
            }

            filtered.Add(eventRecord);
        }

        var ordered = filtered
            .OrderBy(eventRecord => eventRecord.EventStartAtUtc)
            .ToArray();
        var pageItems = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        var items = new List<EventSummaryDto>(pageItems.Length);

        foreach (var eventRecord in pageItems)
        {
            var participants = await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken);
            items.Add(EventDtoMapper.ToSummary(eventRecord, participants.Count(participant => participant.State == EventParticipantState.Joined)));
        }

        return new ListResponse<EventSummaryDto>(items, ordered.Length);
    }

    private static bool MatchesQuery(Event eventRecord, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var value = query.Trim();
        return (!string.IsNullOrWhiteSpace(eventRecord.Title) && eventRecord.Title.Contains(value, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(eventRecord.CuisineTarget) && eventRecord.CuisineTarget.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesCuisine(Event eventRecord, string cuisine, IReadOnlyDictionary<Guid, Restaurant> restaurants)
    {
        if (!string.IsNullOrWhiteSpace(eventRecord.CuisineTarget) && string.Equals(eventRecord.CuisineTarget, cuisine, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return eventRecord.SelectedRestaurantId.HasValue &&
               restaurants.TryGetValue(eventRecord.SelectedRestaurantId.Value, out var restaurant) &&
               restaurant.CuisineTags.Any(tag => string.Equals(tag, cuisine, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesPriceTier(Event eventRecord, PriceTier priceTier, IReadOnlyDictionary<Guid, Restaurant> restaurants) =>
        eventRecord.SelectedRestaurantId.HasValue &&
        restaurants.TryGetValue(eventRecord.SelectedRestaurantId.Value, out var restaurant) &&
        restaurant.PriceTier == priceTier;

    private async Task<bool> MatchesDistanceAsync(
        Event eventRecord,
        IReadOnlyDictionary<Guid, Restaurant> restaurants,
        (double Latitude, double Longitude)? queryPoint,
        UserProfile currentProfile,
        double radiusMiles,
        CancellationToken cancellationToken)
    {
        if (!queryPoint.HasValue)
        {
            return true;
        }

        var location = await ResolveEventLocationAsync(eventRecord, restaurants, currentProfile, cancellationToken);

        if (!location.HasValue)
        {
            return false;
        }

        var distance = RestaurantSearchService.CalculateDistanceMiles(
            queryPoint.Value.Latitude,
            queryPoint.Value.Longitude,
            location.Value.Latitude,
            location.Value.Longitude);

        return distance <= radiusMiles;
    }

    private async Task<(double Latitude, double Longitude)?> ResolveEventLocationAsync(
        Event eventRecord,
        IReadOnlyDictionary<Guid, Restaurant> restaurants,
        UserProfile currentProfile,
        CancellationToken cancellationToken)
    {
        if (eventRecord.SelectedRestaurantId.HasValue &&
            restaurants.TryGetValue(eventRecord.SelectedRestaurantId.Value, out var restaurant) &&
            restaurant.Latitude.HasValue &&
            restaurant.Longitude.HasValue)
        {
            return (restaurant.Latitude.Value, restaurant.Longitude.Value);
        }

        // Cuisine-targeted events fall back to the host's home ZIP when no exact restaurant has been chosen.
        var hostProfile = eventRecord.HostUserId == currentProfile.UserId
            ? currentProfile
            : await profileRepository.GetProfileAsync(eventRecord.HostUserId, cancellationToken);

        if (hostProfile is null)
        {
            return null;
        }

        return await restaurantRepository.GetZipCoordinatesAsync(hostProfile.HomeAreaZipCode, cancellationToken);
    }

    private static bool MatchesAvailability(
        DateTimeOffset eventStartAtUtc,
        IReadOnlyCollection<RecurringAvailabilityWindow> recurringAvailability,
        IReadOnlyCollection<OneOffAvailabilityWindow> oneOffAvailability)
    {
        if (oneOffAvailability.Any(window => eventStartAtUtc >= window.StartsAtUtc && eventStartAtUtc <= window.EndsAtUtc))
        {
            return true;
        }

        var eventTime = TimeOnly.FromDateTime(eventStartAtUtc.UtcDateTime);
        var eventDay = eventStartAtUtc.UtcDateTime.DayOfWeek;

        return recurringAvailability.Any(window =>
            window.DayOfWeek == eventDay &&
            eventTime >= window.StartTime &&
            eventTime <= window.EndTime);
    }
}