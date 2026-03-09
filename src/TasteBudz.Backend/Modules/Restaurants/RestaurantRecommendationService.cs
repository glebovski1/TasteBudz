// Suggestion logic that derives restaurant candidates from explicit query inputs or an event context.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Restaurants;

/// <summary>
/// Produces a small ranked set of restaurant suggestions for event planning.
/// </summary>
public sealed class RestaurantRecommendationService(
    IRestaurantRepository restaurantRepository,
    IEventRepository eventRepository,
    IGroupRepository groupRepository,
    IProfileRepository profileRepository,
    RestaurantSearchService restaurantSearchService)
{
    /// <summary>
    /// Resolves suggestion inputs from the request itself, then enriches them from event or group context when available.
    /// </summary>
    public async Task<IReadOnlyCollection<RestaurantDto>> GetSuggestionsAsync(CurrentUser currentUser, RestaurantSuggestionsQuery query, CancellationToken cancellationToken = default)
    {
        // Raw query values are normalized once so later filters can work with stable values.
        var zipCode = query.ZipCode?.Trim();
        var cuisineTags = NormalizeList(query.CuisineTags);

        if (query.EventId.HasValue)
        {
            // Event context can supply a host ZIP or cuisine target when the client only knows the event id.
            var eventRecord = await eventRepository.GetAsync(query.EventId.Value, cancellationToken)
                ?? throw ApiException.NotFound("The requested event could not be found.");

            await EnsureCanUseEventContextAsync(currentUser, eventRecord, cancellationToken);

            // Event context fills gaps in the query so the client can ask for suggestions with minimal input.
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                var hostProfile = await profileRepository.GetProfileAsync(eventRecord.HostUserId, cancellationToken);
                zipCode = hostProfile?.HomeAreaZipCode;
            }

            if (cuisineTags.Count == 0 && !string.IsNullOrWhiteSpace(eventRecord.CuisineTarget))
            {
                cuisineTags = new[] { eventRecord.CuisineTarget.Trim() };
            }
        }

        if (query.GroupId.HasValue)
        {
            // Group context is now a real contract input, so invalid ids fail instead of being silently ignored.
            var group = await groupRepository.GetAsync(query.GroupId.Value, cancellationToken)
                ?? throw ApiException.NotFound("The requested group could not be found.");

            await EnsureCanUseGroupContextAsync(currentUser, group, cancellationToken);

            if (group.LifecycleState != GroupLifecycleState.Active)
            {
                throw ApiException.Conflict("Only active groups can be used for restaurant suggestions.");
            }

            if (string.IsNullOrWhiteSpace(zipCode))
            {
                // Group suggestions use a lightweight "best known group ZIP" strategy until richer midpoint logic exists.
                var groupReferenceZipCode = await ResolveGroupReferenceZipCodeAsync(group, cancellationToken);

                if (!string.IsNullOrWhiteSpace(groupReferenceZipCode))
                {
                    zipCode = groupReferenceZipCode;
                }
            }
        }

        // The final suggestion list is intentionally deterministic: filter, map, order, then cap at five items.
        var restaurants = await restaurantRepository.ListAsync(cancellationToken);
        var referencePoint = await restaurantSearchService.ResolveReferencePointAsync(zipCode, cancellationToken);
        var suggestions = restaurants
            .Where(restaurant => cuisineTags.Count == 0 || restaurant.CuisineTags.Any(tag => cuisineTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
            .Where(restaurant => !query.RadiusMiles.HasValue || !referencePoint.HasValue ||
                (restaurant.Latitude.HasValue && restaurant.Longitude.HasValue && RestaurantSearchService.CalculateDistanceMiles(referencePoint.Value.Latitude, referencePoint.Value.Longitude, restaurant.Latitude.Value, restaurant.Longitude.Value) <= query.RadiusMiles.Value))
            .Select(restaurant => RestaurantSearchService.ToDto(restaurant, referencePoint))
            .OrderBy(restaurant => restaurant.DistanceMiles ?? double.MaxValue)
            .ThenBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return suggestions;
    }

    private async Task EnsureCanUseEventContextAsync(CurrentUser currentUser, Event eventRecord, CancellationToken cancellationToken)
    {
        if (eventRecord.EventType == EventType.Open || eventRecord.HostUserId == currentUser.UserId)
        {
            return;
        }

        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUser.UserId, cancellationToken);

        if (participant is null || participant.State == EventParticipantState.Removed)
        {
            throw ApiException.NotFound("The requested event could not be found.");
        }
    }

    private async Task EnsureCanUseGroupContextAsync(CurrentUser currentUser, Group group, CancellationToken cancellationToken)
    {
        if (group.OwnerUserId == currentUser.UserId)
        {
            return;
        }

        var membership = await groupRepository.GetMemberAsync(group.Id, currentUser.UserId, cancellationToken);

        if (membership?.State != GroupMemberState.Active)
        {
            throw ApiException.NotFound("The requested group could not be found.");
        }
    }

    /// <summary>
    /// Finds the most useful ZIP to represent a group when the caller did not provide one explicitly.
    /// </summary>
    private async Task<string?> ResolveGroupReferenceZipCodeAsync(Group group, CancellationToken cancellationToken)
    {
        // Prefer the owner ZIP first because ownership is the group's canonical anchor in the domain model.
        var ownerProfile = await profileRepository.GetProfileAsync(group.OwnerUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(ownerProfile?.HomeAreaZipCode))
        {
            return ownerProfile.HomeAreaZipCode;
        }

        var members = await groupRepository.ListMembersAsync(group.Id, cancellationToken);

        // Fall back to the first active member with location data so suggestions still work for incomplete owners.
        foreach (var member in members.Where(member => member.State == GroupMemberState.Active))
        {
            var profile = await profileRepository.GetProfileAsync(member.UserId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(profile?.HomeAreaZipCode))
            {
                return profile.HomeAreaZipCode;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes blanks and duplicate cuisine names so downstream filters see one canonical list.
    /// </summary>
    private static IReadOnlyCollection<string> NormalizeList(IReadOnlyCollection<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
