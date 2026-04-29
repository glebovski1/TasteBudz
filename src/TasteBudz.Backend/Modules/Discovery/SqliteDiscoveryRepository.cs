using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Discovery;

/// <summary>
/// SQLite-backed repository for swipes and reciprocal Bud connections.
/// </summary>
public sealed class SqliteDiscoveryRepository(TasteBudzDbContext dbContext) : IDiscoveryRepository
{
    public async Task<SwipeDecision?> GetSwipeDecisionAsync(Guid actorUserId, Guid subjectUserId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SwipeDecisions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ActorUserId == actorUserId && item.SubjectUserId == subjectUserId, cancellationToken);
        return entity is null ? null : new SwipeDecision(entity.ActorUserId, entity.SubjectUserId, entity.Decision, entity.UpdatedAtUtc);
    }

    public async Task SaveSwipeDecisionAsync(SwipeDecision decision, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SwipeDecisions.FirstOrDefaultAsync(item => item.ActorUserId == decision.ActorUserId && item.SubjectUserId == decision.SubjectUserId, cancellationToken);

        if (entity is null)
        {
            dbContext.SwipeDecisions.Add(new SwipeDecisionEntity
            {
                ActorUserId = decision.ActorUserId,
                SubjectUserId = decision.SubjectUserId,
                Decision = decision.Decision,
                UpdatedAtUtc = decision.UpdatedAtUtc,
            });
        }
        else
        {
            entity.Decision = decision.Decision;
            entity.UpdatedAtUtc = decision.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SwipeDecision>> ListSwipeDecisionsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.SwipeDecisions.AsNoTracking().ToListAsync(cancellationToken))
        .Select(item => new SwipeDecision(item.ActorUserId, item.SubjectUserId, item.Decision, item.UpdatedAtUtc))
        .ToArray();

    public async Task<BudConnection?> GetBudConnectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
    {
        var (lower, higher) = NormalizePair(firstUserId, secondUserId);
        var entity = await dbContext.BudConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserOneId == lower && item.UserTwoId == higher, cancellationToken);
        return entity is null ? null : MapConnection(entity);
    }

    public async Task<BudConnection?> GetBudConnectionByIdAsync(Guid budConnectionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.BudConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == budConnectionId, cancellationToken);
        return entity is null ? null : MapConnection(entity);
    }

    public async Task SaveBudConnectionAsync(BudConnection connection, CancellationToken cancellationToken = default)
    {
        var (lower, higher) = NormalizePair(connection.UserOneId, connection.UserTwoId);
        var entity = await dbContext.BudConnections.FirstOrDefaultAsync(item => item.UserOneId == lower && item.UserTwoId == higher, cancellationToken);

        if (entity is null)
        {
            dbContext.BudConnections.Add(new BudConnectionEntity
            {
                Id = connection.Id,
                UserOneId = lower,
                UserTwoId = higher,
                State = connection.State,
                CreatedAtUtc = connection.CreatedAtUtc,
                EndedAtUtc = connection.EndedAtUtc,
            });
        }
        else
        {
            entity.State = connection.State;
            entity.CreatedAtUtc = connection.CreatedAtUtc;
            entity.EndedAtUtc = connection.EndedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
   
    public async Task<IReadOnlyCollection<BudConnection>> ListBudConnectionsAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.BudConnections.AsNoTracking().ToListAsync(cancellationToken))
        .Select(MapConnection)
        .ToArray();

    public async Task RemoveBudConnectionAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default)
    {
        var (lower, higher) = NormalizePair(firstUserId, secondUserId);
        var entity = await dbContext.BudConnections
            .FirstOrDefaultAsync(item => item.UserOneId == lower && item.UserTwoId == higher, cancellationToken);

        if (entity is not null)
        {
            entity.State = BudConnectionState.Removed;
            entity.EndedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static BudConnection MapConnection(BudConnectionEntity entity) =>
        new(
            entity.Id,
            entity.UserOneId,
            entity.UserTwoId,
            entity.State,
            entity.CreatedAtUtc,
            entity.EndedAtUtc);

    private static (Guid Lower, Guid Higher) NormalizePair(Guid firstUserId, Guid secondUserId) =>
        firstUserId.CompareTo(secondUserId) <= 0 ? (firstUserId, secondUserId) : (secondUserId, firstUserId);
}
