// In-memory event repository used by the MVP runtime and automated tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Stores events and participant rows inside the shared in-memory store.
/// </summary>
public sealed class InMemoryEventRepository(InMemoryTasteBudzStore store) : IEventRepository
{
    public Task<Event?> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Events.TryGetValue(eventId, out var eventRecord);
            return Task.FromResult(eventRecord);
        }
    }

    public Task<IReadOnlyCollection<Event>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Events.Values
                .OrderBy(eventRecord => eventRecord.EventStartAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Event>>(items);
        }
    }

    public Task SaveAsync(Event eventRecord, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Events[eventRecord.Id] = eventRecord;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<EventParticipant>> ListParticipantsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.EventParticipants.Values
                .Where(participant => participant.EventId == eventId)
                .OrderBy(participant => participant.JoinedAtUtc ?? participant.InvitedAtUtc ?? DateTimeOffset.MaxValue)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EventParticipant>>(items);
        }
    }

    public Task<EventParticipant?> GetParticipantAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventParticipants.TryGetValue(ToKey(eventId, userId), out var participant);
            return Task.FromResult(participant);
        }
    }

    public Task SaveParticipantAsync(EventParticipant participant, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.EventParticipants[ToKey(participant.EventId, participant.UserId)] = participant;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<EventParticipant>> ListParticipantsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.EventParticipants.Values
                .Where(participant => participant.UserId == userId)
                .OrderByDescending(participant => participant.JoinedAtUtc ?? participant.InvitedAtUtc ?? DateTimeOffset.MinValue)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<EventParticipant>>(items);
        }
    }

    private static string ToKey(Guid eventId, Guid userId) => $"{eventId:N}:{userId:N}";
}