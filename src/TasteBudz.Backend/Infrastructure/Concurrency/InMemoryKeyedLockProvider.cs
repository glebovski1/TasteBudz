// In-memory keyed lock implementation used to protect race-prone workflows in the MVP.
using System.Collections.Concurrent;

namespace TasteBudz.Backend.Infrastructure.Concurrency;

/// <summary>
/// Uses a per-key semaphore so conflicting commands for the same aggregate run one at a time.
/// </summary>
public sealed class InMemoryKeyedLockProvider : IKeyedLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);

    public async Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        var semaphore = locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    /// <summary>
    /// Releases the semaphore when the caller exits an <c>await using</c> scope.
    /// </summary>
    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}