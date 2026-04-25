using TasteBudz.Backend.Domain;
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
        CancellationToken cancellationToken)
    {
        if (isPrivileged || eventRecord.HostUserId == currentUserId || eventRecord.EventType == EventType.Open)
        {
            return true;
        }

        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);
        return participant is not null && participant.State != EventParticipantState.Removed;
    }
}
