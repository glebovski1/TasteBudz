using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// SQLite-backed repository for events and event participation state.
/// </summary>
public sealed class SqliteEventRepository(TasteBudzDbContext dbContext) : IEventRepository
{
    public async Task<Event?> GetAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        return entity is null ? null : MapEvent(entity);
    }

    public async Task<IReadOnlyCollection<Event>> ListAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Events.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapEvent)
        .OrderBy(item => item.EventStartAtUtc)
        .ThenBy(item => item.Id)
        .ToArray();

    public async Task SaveAsync(Event eventRecord, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events.FirstOrDefaultAsync(item => item.Id == eventRecord.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.Events.Add(ToEntity(eventRecord));
        }
        else
        {
            entity.HostUserId = eventRecord.HostUserId;
            entity.Title = eventRecord.Title;
            entity.EventType = eventRecord.EventType;
            entity.Status = eventRecord.Status;
            entity.EventStartAtUtc = eventRecord.EventStartAtUtc;
            entity.DecisionAtUtc = eventRecord.DecisionAtUtc;
            entity.Capacity = eventRecord.Capacity;
            entity.MinParticipantsToRun = eventRecord.MinParticipantsToRun;
            entity.SelectedRestaurantId = eventRecord.SelectedRestaurantId;
            entity.CuisineTarget = eventRecord.CuisineTarget;
            entity.GroupId = eventRecord.GroupId;
            entity.CancellationReason = eventRecord.CancellationReason;
            entity.CreatedAtUtc = eventRecord.CreatedAtUtc;
            entity.UpdatedAtUtc = eventRecord.UpdatedAtUtc;
            entity.CancelledAtUtc = eventRecord.CancelledAtUtc;
            entity.CompletedAtUtc = eventRecord.CompletedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventParticipant>> ListParticipantsAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        (await dbContext.EventParticipants
            .AsNoTracking()
            .Where(participant => participant.EventId == eventId)
            .ToListAsync(cancellationToken))
        .Select(MapParticipant)
        .OrderBy(participant => participant.JoinedAtUtc ?? participant.InvitedAtUtc ?? DateTimeOffset.MaxValue)
        .ToArray();

    public async Task<EventParticipant?> GetParticipantAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(participant => participant.EventId == eventId && participant.UserId == userId, cancellationToken);
        return entity is null ? null : MapParticipant(entity);
    }

    public async Task SaveParticipantAsync(EventParticipant participant, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EventParticipants.FirstOrDefaultAsync(item => item.EventId == participant.EventId && item.UserId == participant.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.EventParticipants.Add(ToEntity(participant));
        }
        else
        {
            entity.State = participant.State;
            entity.InvitedAtUtc = participant.InvitedAtUtc;
            entity.JoinedAtUtc = participant.JoinedAtUtc;
            entity.RespondedAtUtc = participant.RespondedAtUtc;
            entity.LeftAtUtc = participant.LeftAtUtc;
            entity.RemovedAtUtc = participant.RemovedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EventParticipant>> ListParticipantsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.EventParticipants
            .AsNoTracking()
            .Where(participant => participant.UserId == userId)
            .ToListAsync(cancellationToken))
        .Select(MapParticipant)
        .OrderByDescending(participant => participant.JoinedAtUtc ?? participant.InvitedAtUtc ?? DateTimeOffset.MinValue)
        .ToArray();

    private static Event MapEvent(EventEntity entity) =>
        new(
            entity.Id,
            entity.HostUserId,
            entity.Title,
            entity.EventType,
            entity.Status,
            entity.EventStartAtUtc,
            entity.DecisionAtUtc,
            entity.Capacity,
            entity.MinParticipantsToRun,
            entity.SelectedRestaurantId,
            entity.CuisineTarget,
            entity.GroupId,
            entity.CancellationReason,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CancelledAtUtc,
            entity.CompletedAtUtc);

    private static EventParticipant MapParticipant(EventParticipantEntity entity) =>
        new(
            entity.EventId,
            entity.UserId,
            entity.State,
            entity.InvitedAtUtc,
            entity.JoinedAtUtc,
            entity.RespondedAtUtc,
            entity.LeftAtUtc,
            entity.RemovedAtUtc);

    private static EventEntity ToEntity(Event item) =>
        new()
        {
            Id = item.Id,
            HostUserId = item.HostUserId,
            Title = item.Title,
            EventType = item.EventType,
            Status = item.Status,
            EventStartAtUtc = item.EventStartAtUtc,
            DecisionAtUtc = item.DecisionAtUtc,
            Capacity = item.Capacity,
            MinParticipantsToRun = item.MinParticipantsToRun,
            SelectedRestaurantId = item.SelectedRestaurantId,
            CuisineTarget = item.CuisineTarget,
            GroupId = item.GroupId,
            CancellationReason = item.CancellationReason,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
            CancelledAtUtc = item.CancelledAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
        };

    private static EventParticipantEntity ToEntity(EventParticipant item) =>
        new()
        {
            EventId = item.EventId,
            UserId = item.UserId,
            State = item.State,
            InvitedAtUtc = item.InvitedAtUtc,
            JoinedAtUtc = item.JoinedAtUtc,
            RespondedAtUtc = item.RespondedAtUtc,
            LeftAtUtc = item.LeftAtUtc,
            RemovedAtUtc = item.RemovedAtUtc,
        };
}
