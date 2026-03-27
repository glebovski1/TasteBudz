namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Fallback transaction runner used by in-memory unit tests that do not spin up a DbContext.
/// </summary>
public sealed class NoOpPersistenceTransactionRunner : IPersistenceTransactionRunner
{
    public static NoOpPersistenceTransactionRunner Instance { get; } = new();

    private NoOpPersistenceTransactionRunner()
    {
    }

    public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();
}
