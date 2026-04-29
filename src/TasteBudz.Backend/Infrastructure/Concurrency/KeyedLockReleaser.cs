// In-memory keyed lock implementation used to protect race-prone workflows in the MVP.
using System.Collections.Concurrent;

namespace TasteBudz.Backend.Infrastructure.Concurrency;


/// <summary>
/// Releases the semaphore when the caller exits an <c>await using</c> scope.
/// </summary>
internal sealed class KeyedLockReleaser(SemaphoreSlim semaphore) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        semaphore.Release();
        return ValueTask.CompletedTask;
    }
}
