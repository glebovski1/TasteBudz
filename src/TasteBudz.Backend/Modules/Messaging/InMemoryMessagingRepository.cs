// In-memory messaging repository used by the MVP runtime and tests.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// Stores chat threads by scope and messages by identifier in the shared in-memory store.
/// </summary>
public sealed class InMemoryMessagingRepository(InMemoryTasteBudzStore store) : IMessagingRepository
{
    public Task<ChatThread?> GetThreadByScopeAsync(ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ChatThreads.TryGetValue(ToThreadKey(scopeType, scopeId), out var thread);
            return Task.FromResult(thread);
        }
    }

    public Task SaveThreadAsync(ChatThread thread, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ChatThreads[ToThreadKey(thread.ScopeType, thread.ScopeId)] = thread;
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyCollection<ChatMessage>> ListMessagesAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.ChatMessages.Values
                .Where(message => message.ThreadId == threadId)
                .OrderBy(message => message.CreatedAtUtc)
                .ThenBy(message => message.Id)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ChatMessage>>(items);
        }
    }

    public Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.ChatMessages[message.Id] = message;
            return Task.CompletedTask;
        }
    }

    private static string ToThreadKey(ChatScopeType scopeType, Guid scopeId) => $"{scopeType}:{scopeId:N}";
}
