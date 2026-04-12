using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Payments;

public sealed class SqliteCheckoutSessionRepository(TasteBudzDbContext dbContext) : ICheckoutSessionRepository
{
    public async Task<CheckoutSession?> GetAsync(Guid checkoutSessionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.CheckoutSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == checkoutSessionId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyCollection<CheckoutSession>> ListForEventUserAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.CheckoutSessions
            .AsNoTracking()
            .Where(item => item.EventId == eventId && item.UserId == userId)
            .ToListAsync(cancellationToken))
        .Select(Map)
        .OrderByDescending(item => item.CreatedAtUtc)
        .ThenByDescending(item => item.Id)
        .ToArray();

    public async Task SaveAsync(CheckoutSession session, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.CheckoutSessions.FirstOrDefaultAsync(item => item.Id == session.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.CheckoutSessions.Add(ToEntity(session));
        }
        else
        {
            entity.EventId = session.EventId;
            entity.UserId = session.UserId;
            entity.Status = session.Status;
            entity.Currency = session.Currency;
            entity.SubtotalCents = session.SubtotalCents;
            entity.DiscountCents = session.DiscountCents;
            entity.TotalCents = session.TotalCents;
            entity.CreatedAtUtc = session.CreatedAtUtc;
            entity.UpdatedAtUtc = session.UpdatedAtUtc;
            entity.CompletedAtUtc = session.CompletedAtUtc;
            entity.CancelledAtUtc = session.CancelledAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CheckoutSession Map(CheckoutSessionEntity entity) =>
        new(
            entity.Id,
            entity.EventId,
            entity.UserId,
            entity.Status,
            entity.Currency,
            entity.SubtotalCents,
            entity.DiscountCents,
            entity.TotalCents,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CompletedAtUtc,
            entity.CancelledAtUtc);

    private static CheckoutSessionEntity ToEntity(CheckoutSession session) =>
        new()
        {
            Id = session.Id,
            EventId = session.EventId,
            UserId = session.UserId,
            Status = session.Status,
            Currency = session.Currency,
            SubtotalCents = session.SubtotalCents,
            DiscountCents = session.DiscountCents,
            TotalCents = session.TotalCents,
            CreatedAtUtc = session.CreatedAtUtc,
            UpdatedAtUtc = session.UpdatedAtUtc,
            CompletedAtUtc = session.CompletedAtUtc,
            CancelledAtUtc = session.CancelledAtUtc,
        };
}
