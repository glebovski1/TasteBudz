// Persistence boundary for events and their participant records.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Stores event aggregates together with user participation state.
/// </summary>
public interface IEventRepository
{
    Task<Event?> GetAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Event>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Event eventRecord, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventParticipant>> ListParticipantsAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<EventParticipant?> GetParticipantAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    Task SaveParticipantAsync(EventParticipant participant, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventParticipant>> ListParticipantsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}