using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// SQLite-backed repository for chat threads and immutable messages.
/// </summary>
public sealed class SqliteMessagingRepository(
    TasteBudzDbContext dbContext,
    IPersistenceExceptionClassifier exceptionClassifier) : IMessagingRepository
{
    public async Task<ChatThread?> GetThreadByScopeAsync(ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ChatThreads
            .AsNoTracking()
            .FirstOrDefaultAsync(thread => thread.ScopeType == scopeType && thread.ScopeId == scopeId, cancellationToken);
        return entity is null ? null : new ChatThread(entity.Id, entity.ScopeType, entity.ScopeId, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<ChatThread>> ListThreadsByScopeTypeAsync(ChatScopeType scopeType, CancellationToken cancellationToken = default) =>
        (await dbContext.ChatThreads
            .AsNoTracking()
            .Where(thread => thread.ScopeType == scopeType)
            .ToListAsync(cancellationToken))
        .Select(thread => new ChatThread(thread.Id, thread.ScopeType, thread.ScopeId, thread.CreatedAtUtc))
        .OrderBy(thread => thread.CreatedAtUtc)
        .ThenBy(thread => thread.ScopeId)
        .ToArray();

    public async Task SaveThreadAsync(ChatThread thread, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.ChatThreads.FirstOrDefaultAsync(item => item.ScopeType == thread.ScopeType && item.ScopeId == thread.ScopeId, cancellationToken);

        if (existing is null)
        {
            var entity = new ChatThreadEntity
            {
                Id = thread.Id,
                ScopeType = thread.ScopeType,
                ScopeId = thread.ScopeId,
                CreatedAtUtc = thread.CreatedAtUtc,
            };
            dbContext.ChatThreads.Add(entity);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exceptionClassifier.IsUniqueConstraintViolation(exception))
            {
                dbContext.Entry(entity).State = EntityState.Detached;
            }

            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ChatMessage>> ListMessagesAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        (await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.ThreadId == threadId)
            .ToListAsync(cancellationToken))
        .Select(message => new ChatMessage(message.Id, message.ThreadId, message.SenderUserId, message.Body, message.CreatedAtUtc))
        .OrderBy(message => message.CreatedAtUtc)
        .ThenBy(message => message.Id)
        .ToArray();

    public async Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ChatMessages.FirstOrDefaultAsync(item => item.Id == message.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.ChatMessages.Add(new ChatMessageEntity
            {
                Id = message.Id,
                ThreadId = message.ThreadId,
                SenderUserId = message.SenderUserId,
                Body = message.Body,
                CreatedAtUtc = message.CreatedAtUtc,
            });
        }
        else
        {
            entity.ThreadId = message.ThreadId;
            entity.SenderUserId = message.SenderUserId;
            entity.Body = message.Body;
            entity.CreatedAtUtc = message.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
