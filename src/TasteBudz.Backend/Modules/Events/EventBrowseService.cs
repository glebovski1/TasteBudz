// Browse logic for visible events, including search, availability, distance, and lifecycle filtering.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Discovery;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.Modules.Restaurants;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Builds the event browse response from event, profile, and restaurant data.
/// </summary>
public sealed class EventBrowseService(
    IEventRepository eventRepository,
    IRestaurantRepository restaurantRepository,
    IProfileRepository profileRepository,
    IDiscoveryRepository discoveryRepository,
    EventLifecycleService lifecycleService,
    IRestaurantOperationsRepository restaurantOperationsRepository,
    IFeatureFlagService featureFlagService)
{
    public async Task<ListResponse<EventSummaryDto>> BrowseAsync(Guid currentUserId, BrowseEventsQuery query, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.ListAsync(cancellationToken);
        var synchronized = new List<Event>(events.Count);

        foreach (var eventRecord in events)
        {
            synchronized.Add(await lifecycleService.SynchronizeAsync(eventRecord, cancellationToken));
        }

        var restaurants = (await restaurantRepository.ListAsync(cancellationToken: cancellationToken)).ToDictionary(restaurant => restaurant.Id);
        var currentProfile = await profileRepository.GetProfileAsync(currentUserId, cancellationToken)
            ?? throw ApiException.NotFound("The current profile could not be found.");
        var referenceZipCode = !string.IsNullOrWhiteSpace(query.ZipCode)
            ? query.ZipCode.Trim()
            : query.Recommended ? currentProfile.HomeAreaZipCode : null;
        var referencePoint = string.IsNullOrWhiteSpace(referenceZipCode)
            ? null
            : await restaurantRepository.GetZipCoordinatesAsync(referenceZipCode, cancellationToken);
        var recurringAvailability = query.AvailabilityOnly
            ? await profileRepository.ListRecurringAvailabilityAsync(currentUserId, cancellationToken)
            : Array.Empty<RecurringAvailabilityWindow>();
        var oneOffAvailability = query.AvailabilityOnly
            ? await profileRepository.ListOneOffAvailabilityAsync(currentUserId, cancellationToken)
            : Array.Empty<OneOffAvailabilityWindow>();
        var currentPreferences = query.Recommended
            ? await profileRepository.GetPreferencesAsync(currentUserId, cancellationToken)
            : null;
        var budUserIds = query.Recommended
            ? await ListConnectedBudUserIdsAsync(currentUserId, cancellationToken)
            : [];

        var filtered = new List<BrowseCandidate>();

        foreach (var eventRecord in synchronized)
        {
            if (!await EventVisibilityPolicy.CanViewAsync(
                    currentUserId,
                    isPrivileged: false,
                    eventRecord,
                    eventRepository,
                    cancellationToken))
            {
                continue;
            }

            if (query.Recommended && eventRecord.EventType != EventType.Open)
            {
                continue;
            }

            if (query.EventType.HasValue && eventRecord.EventType != query.EventType.Value)
            {
                continue;
            }

            if (query.Recommended && eventRecord.Status != EventStatus.Open)
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

            if (query.GroupLinked.HasValue && eventRecord.GroupId.HasValue != query.GroupLinked.Value)
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

            var participants = await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken);
            var activeParticipants = participants.Count(participant => participant.State == EventParticipantState.Joined);

            if (query.Recommended && activeParticipants >= eventRecord.Capacity)
            {
                continue;
            }

            var distanceMiles = await GetDistanceMilesAsync(eventRecord, restaurants, referencePoint, currentProfile, cancellationToken);

            if (query.RadiusMiles.HasValue &&
                referencePoint.HasValue &&
                (!distanceMiles.HasValue || distanceMiles.Value > query.RadiusMiles.Value))
            {
                continue;
            }

            var matchingCuisineCount = query.Recommended
                ? CountMatchingCuisinePreferences(eventRecord, restaurants, currentPreferences)
                : 0;
            var matchingBudzCount = query.Recommended
                ? CountMatchingBudz(participants, budUserIds, currentUserId)
                : 0;

            filtered.Add(new BrowseCandidate(
                eventRecord,
                activeParticipants,
                distanceMiles,
                matchingCuisineCount,
                matchingBudzCount,
                query.Recommended
                    ? ComputeRecommendationScore(distanceMiles, matchingCuisineCount, matchingBudzCount)
                    : 0));
        }

        var ordered = (query.Recommended
                ? filtered
                    .OrderByDescending(candidate => candidate.RecommendationScore)
                    .ThenBy(candidate => candidate.Event.EventStartAtUtc)
                    .ThenBy(candidate => candidate.Event.Id)
                : filtered
                    .OrderBy(candidate => candidate.Event.EventStartAtUtc)
                    .ThenBy(candidate => candidate.Event.Id))
            .ToArray();
        var pageItems = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        var items = new List<EventSummaryDto>(pageItems.Length);

        foreach (var candidate in pageItems)
        {
            var cardStatus = await GetRestaurantCardStatusAsync(candidate.Event.Id, cancellationToken);
            items.Add(EventDtoMapper.ToSummary(
                candidate.Event,
                candidate.ActiveParticipants,
                candidate.DistanceMiles,
                candidate.MatchingCuisineCount,
                candidate.MatchingBudzCount,
                cardStatus.HasActiveSlotReservation,
                cardStatus.IsDiscountActive,
                cardStatus.DiscountPercent));
        }

        return new ListResponse<EventSummaryDto>(items, ordered.Length);
    }

    private async Task<EventRestaurantCardStatus> GetRestaurantCardStatusAsync(Guid eventId, CancellationToken cancellationToken)
    {
        if (!featureFlagService.IsRestaurantsOperationsEnabled() ||
            !featureFlagService.IsRestaurantsSlotsEnabled())
        {
            return EventRestaurantCardStatus.Empty;
        }

        var reservation = await restaurantOperationsRepository.GetActiveReservationForEventAsync(eventId, cancellationToken);

        if (reservation is null)
        {
            return EventRestaurantCardStatus.Empty;
        }

        if (!featureFlagService.IsRestaurantsDiscountsEnabled())
        {
            return new EventRestaurantCardStatus(true, false, null);
        }

        var activation = await restaurantOperationsRepository.GetDiscountActivationAsync(reservation.Id, cancellationToken);

        if (activation?.IsActive != true)
        {
            return new EventRestaurantCardStatus(true, false, null);
        }

        var slot = await restaurantOperationsRepository.GetSlotAsync(reservation.SlotId, cancellationToken);
        return new EventRestaurantCardStatus(true, true, slot?.DiscountPercent);
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

    private async Task<double?> GetDistanceMilesAsync(
        Event eventRecord,
        IReadOnlyDictionary<Guid, Restaurant> restaurants,
        (double Latitude, double Longitude)? queryPoint,
        UserProfile currentProfile,
        CancellationToken cancellationToken)
    {
        if (!queryPoint.HasValue)
        {
            return null;
        }

        var location = await ResolveEventLocationAsync(eventRecord, restaurants, currentProfile, cancellationToken);

        if (!location.HasValue)
        {
            return null;
        }

        return RestaurantSearchService.CalculateDistanceMiles(
            queryPoint.Value.Latitude,
            queryPoint.Value.Longitude,
            location.Value.Latitude,
            location.Value.Longitude);
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

    private static int CountMatchingCuisinePreferences(
        Event eventRecord,
        IReadOnlyDictionary<Guid, Restaurant> restaurants,
        UserPreferences? preferences)
    {
        if (preferences is null || preferences.CuisineTags.Count == 0)
        {
            return 0;
        }

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var preference in preferences.CuisineTags.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!string.IsNullOrWhiteSpace(eventRecord.CuisineTarget) &&
                eventRecord.CuisineTarget.Contains(preference, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(preference);
            }
        }

        if (eventRecord.SelectedRestaurantId.HasValue &&
            restaurants.TryGetValue(eventRecord.SelectedRestaurantId.Value, out var restaurant))
        {
            foreach (var tag in restaurant.CuisineTags)
            {
                if (preferences.CuisineTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    matches.Add(tag);
                }
            }
        }

        return matches.Count;
    }

    private static int CountMatchingBudz(
        IReadOnlyCollection<EventParticipant> participants,
        HashSet<Guid> budUserIds,
        Guid currentUserId) =>
        participants
            .Where(participant => participant.State == EventParticipantState.Joined)
            .Select(participant => participant.UserId)
            .Where(userId => userId != currentUserId && budUserIds.Contains(userId))
            .Distinct()
            .Count();

    private async Task<HashSet<Guid>> ListConnectedBudUserIdsAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var connections = await discoveryRepository.ListBudConnectionsAsync(cancellationToken);
        return connections
            .Where(connection => connection.State == BudConnectionState.Connected)
            .Where(connection => connection.UserOneId == currentUserId || connection.UserTwoId == currentUserId)
            .Select(connection => connection.UserOneId == currentUserId ? connection.UserTwoId : connection.UserOneId)
            .ToHashSet();
    }

    private static double ComputeRecommendationScore(double? distanceMiles, int matchingCuisineCount, int matchingBudzCount)
    {
        var score = 0d;

        if (distanceMiles.HasValue)
        {
            score += Math.Max(0, 30 - Math.Min(distanceMiles.Value, 30));
        }

        score += Math.Min(matchingCuisineCount, 3) * 20d;
        score += Math.Min(matchingBudzCount, 3) * 35d;
        return score;
    }

    private sealed record BrowseCandidate(
        Event Event,
        int ActiveParticipants,
        double? DistanceMiles,
        int MatchingCuisineCount,
        int MatchingBudzCount,
        double RecommendationScore);

    private sealed record EventRestaurantCardStatus(
        bool HasActiveSlotReservation,
        bool IsDiscountActive,
        int? DiscountPercent)
    {
        public static EventRestaurantCardStatus Empty { get; } = new(false, false, null);
    }
}
