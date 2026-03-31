namespace TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Executes multi-repository workflows inside a shared persistence transaction when available.
/// </summary>
public interface IPersistenceTransactionRunner
{
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
