using Microsoft.EntityFrameworkCore;

namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Opens a database transaction when one is not already active on the shared DbContext.
/// </summary>
public sealed class SqliteTransactionRunner(TasteBudzDbContext dbContext) : IPersistenceTransactionRunner
{
    public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            async () =>
            {
                await action();
                return true;
            },
            cancellationToken);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await action();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
