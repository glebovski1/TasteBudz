using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Shared event visibility rules for browse, detail, participation, and feedback surfaces.
/// </summary>
internal static class EventVisibilityPolicy
{
    public static async Task<bool> CanViewAsync(
        Guid currentUserId,
        bool isPrivileged,
        Event eventRecord,
        IEventRepository eventRepository,
        IProfileRepository profileRepository,
        CancellationToken cancellationToken)
    {
        if (isPrivileged || eventRecord.HostUserId == currentUserId)
        {
            return true;
        }

        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);

        if (participant is not null && participant.State != EventParticipantState.Removed)
        {
            return true;
        }

        if (eventRecord.EventType != EventType.Open)
        {
            return false;
        }

        var hostPrivacy = await profileRepository.GetPrivacySettingsAsync(eventRecord.HostUserId, cancellationToken);

        if (hostPrivacy?.DiscoveryEnabled == false)
        {
            return false;
        }

        return await profileRepository.GetBlockAsync(currentUserId, eventRecord.HostUserId, cancellationToken) is null &&
               await profileRepository.GetBlockAsync(eventRecord.HostUserId, currentUserId, cancellationToken) is null;
    }
}
