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

        if (hasRestaurant == hasCuisine)
        {
            throw ApiException.BadRequest("Exactly one of selectedRestaurantId or cuisineTarget must be set.");
        }
    }

    internal static async Task EnsureNotBlockedAsync(
        IProfileRepository profileRepository,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        if (await profileRepository.GetBlockAsync(firstUserId, secondUserId, cancellationToken) is not null ||
            await profileRepository.GetBlockAsync(secondUserId, firstUserId, cancellationToken) is not null)
        {
            throw ApiException.Forbidden("Blocking prevents event invitations between these users.");
        }
    }
}