// Serializes work against a logical application key, such as a single event id.
namespace TasteBudz.Backend.Infrastructure.Concurrency;

/// <summary>
/// Provides coarse-grained async locking for workflows that must not interleave in memory.
/// </summary>
public interface IKeyedLockProvider
{
    Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default);
}