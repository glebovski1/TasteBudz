// Persistence boundary for scoped chat threads and messages.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// Stores reusable chat threads together with immutable messages.
/// </summary>
public interface IMessagingRepository
{
    Task<ChatThread?> GetThreadByScopeAsync(ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default);

    Task SaveThreadAsync(ChatThread thread, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChatMessage>> ListMessagesAsync(Guid threadId, CancellationToken cancellationToken = default);

    Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}
