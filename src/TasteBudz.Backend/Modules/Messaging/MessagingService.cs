// Messaging core shared by event chat and group chat.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Moderation;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// Owns scope-based messaging access checks, history retrieval, and message writes.
/// </summary>
public sealed class MessagingService(
    IMessagingRepository messagingRepository,
    IEventRepository eventRepository,
    IGroupRepository groupRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    RestrictionService restrictionService,
    IClock clock)
{
    public static string GetChannelName(ChatScopeType scopeType, Guid scopeId) => $"{scopeType}:{scopeId:N}";

    public async Task<string> JoinScopeAsync(CurrentUser currentUser, ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessScopeAsync(currentUser.UserId, scopeType, scopeId, forSend: false, cancellationToken);
        await GetOrCreateThreadAsync(scopeType, scopeId, cancellationToken);
        return GetChannelName(scopeType, scopeId);
    }

    public Task<CursorPageResponse<ChatMessageDto>> ListEventMessagesAsync(Guid currentUserId, Guid eventId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUserId, ChatScopeType.Event, eventId, query, cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListGroupMessagesAsync(Guid currentUserId, Guid groupId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUserId, ChatScopeType.Group, groupId, query, cancellationToken);

    public async Task<ChatMessageDto> SendAsync(CurrentUser currentUser, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var scopeType = request.ScopeType ?? throw ApiException.BadRequest("scopeType is required.");
        var scopeId = request.ScopeId ?? throw ApiException.BadRequest("scopeId is required.");
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? throw ApiException.BadRequest("body is required.")
            : request.Body.Trim();

        await EnsureCanAccessScopeAsync(currentUser.UserId, scopeType, scopeId, forSend: true, cancellationToken);

        var thread = await GetOrCreateThreadAsync(scopeType, scopeId, cancellationToken);
        var message = new ChatMessage(Guid.NewGuid(), thread.Id, currentUser.UserId, body, clock.UtcNow);
        await messagingRepository.SaveMessageAsync(message, cancellationToken);

        return await MapMessageAsync(message, cancellationToken);
    }

    private async Task<CursorPageResponse<ChatMessageDto>> ListMessagesAsync(
        Guid currentUserId,
        ChatScopeType scopeType,
        Guid scopeId,
        ChatHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureCanAccessScopeAsync(currentUserId, scopeType, scopeId, forSend: false, cancellationToken);
        var thread = await GetOrCreateThreadAsync(scopeType, scopeId, cancellationToken);
        var messages = await messagingRepository.ListMessagesAsync(thread.Id, cancellationToken);
        var ordered = messages
            .OrderBy(message => message.CreatedAtUtc)
            .ThenBy(message => message.Id)
            .ToArray();
        var startIndex = 0;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!Guid.TryParse(query.Cursor, out var cursorId))
            {
                throw ApiException.BadRequest("cursor must be a valid message id.");
            }

            startIndex = Array.FindIndex(ordered, message => message.Id == cursorId);

            if (startIndex < 0)
            {
                throw ApiException.BadRequest("cursor does not match a message in this thread.");
            }

            startIndex += 1;
        }

        var page = ordered
            .Skip(startIndex)
            .Take(query.PageSize)
            .ToArray();
        var items = new List<ChatMessageDto>(page.Length);

        foreach (var message in page)
        {
            items.Add(await MapMessageAsync(message, cancellationToken));
        }

        var nextCursor = startIndex + page.Length < ordered.Length
            ? page.Last().Id.ToString()
            : null;

        return new CursorPageResponse<ChatMessageDto>(items, nextCursor);
    }

    private async Task<ChatThread> GetOrCreateThreadAsync(ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        if (scopeType == ChatScopeType.Direct)
        {
            throw ApiException.NotFound("Direct chat is not part of the current MVP backend surface.");
        }

        var existing = await messagingRepository.GetThreadByScopeAsync(scopeType, scopeId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new ChatThread(Guid.NewGuid(), scopeType, scopeId, clock.UtcNow);
        await messagingRepository.SaveThreadAsync(created, cancellationToken);
        return created;
    }

    private async Task EnsureCanAccessScopeAsync(Guid currentUserId, ChatScopeType scopeType, Guid scopeId, bool forSend, CancellationToken cancellationToken)
    {
        switch (scopeType)
        {
            case ChatScopeType.Event:
                var eventRecord = await eventRepository.GetAsync(scopeId, cancellationToken)
                    ?? throw ApiException.NotFound("The requested chat scope could not be found.");
                var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);

                if (participant?.State != EventParticipantState.Joined)
                {
                    throw ApiException.NotFound("The requested chat scope could not be found.");
                }

                break;

            case ChatScopeType.Group:
                var group = await groupRepository.GetAsync(scopeId, cancellationToken)
                    ?? throw ApiException.NotFound("The requested chat scope could not be found.");
                var membership = await groupRepository.GetMemberAsync(group.Id, currentUserId, cancellationToken);

                if (group.LifecycleState != GroupLifecycleState.Active || membership?.State != GroupMemberState.Active)
                {
                    throw ApiException.NotFound("The requested chat scope could not be found.");
                }

                break;

            default:
                throw ApiException.NotFound("The requested chat scope could not be found.");
        }

        if (forSend)
        {
            await restrictionService.EnsureNotRestrictedAsync(currentUserId, RestrictionScope.ChatSend, "You are currently restricted from sending chat messages.", cancellationToken);
        }
    }

    private async Task<ChatMessageDto> MapMessageAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        var account = await authRepository.GetByIdAsync(message.SenderUserId, cancellationToken)
            ?? throw ApiException.NotFound("The message sender could not be found.");
        var profile = await profileRepository.GetProfileAsync(message.SenderUserId, cancellationToken);

        return new ChatMessageDto(
            message.Id,
            message.SenderUserId,
            account.Username,
            profile?.DisplayName ?? account.Username,
            message.Body,
            message.CreatedAtUtc);
    }
}
