// Shared event-policy helpers used across multiple event workflow services.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Encapsulates cross-cutting event rules that do not require repository access beyond simple checks.
/// </summary>
internal static class EventPolicy
{
    internal static string GetLockKey(Guid eventId) => $"event:{eventId:N}";

    internal static bool IsPrivileged(CurrentUser currentUser) =>
        currentUser.IsInRole(UserRole.Moderator) || currentUser.IsInRole(UserRole.Admin);

    internal static DateTimeOffset CalculateDecisionAt(EventType eventType, DateTimeOffset eventStartAtUtc) =>
        eventType == EventType.Open
            ? eventStartAtUtc.AddMinutes(-15)
            : eventStartAtUtc.AddHours(-24);

    internal static void EnsureValidLocationSelection(Guid? selectedRestaurantId, string? cuisineTarget)
    {
        var hasRestaurant = selectedRestaurantId.HasValue;
        var hasCuisine = !string.IsNullOrWhiteSpace(cuisineTarget);

        if (!hasRestaurant && !hasCuisine)
        {
            throw ApiException.BadRequest("Please select a restaurant or enter a cuisine target.");
        }

        if (hasRestaurant && hasCuisine)
        {
            throw ApiException.BadRequest("Provide either a restaurant or a cuisine target, not both.");
        }
    }

    internal static async Task EnsureNotBlockedAsync(
        IProfileRepository profileRepository,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (await BlockingPolicy.HasBlockBetweenAsync(profileRepository, firstUserId, secondUserId, cancellationToken))
        {
            throw ApiException.Forbidden("Blocking prevents event invitations between these users.");
        }
    }

    internal static async Task<bool> HasBlockedLiveParticipantAsync(
        IProfileRepository profileRepository,
        Event eventRecord,
        IReadOnlyCollection<EventParticipant> participants,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (eventRecord.Status == EventStatus.Completed || eventRecord.HostUserId == currentUserId)
        {
            return false;
        }

        var activeUserIds = participants
            .Where(participant => participant.State == EventParticipantState.Joined)
            .Select(participant => participant.UserId)
            .Append(eventRecord.HostUserId);

        return await BlockingPolicy.HasBlockWithAnyAsync(profileRepository, currentUserId, activeUserIds, cancellationToken);
    }
}
