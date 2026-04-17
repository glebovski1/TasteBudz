// Messaging core shared by event chat and group chat.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.FeatureFlags;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Discovery;
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
    IDiscoveryRepository discoveryRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    IFeatureFlagService featureFlagService,
    RestrictionService restrictionService,
    IClock clock)
{
    public static string GetChannelName(ChatScopeType scopeType, Guid scopeId) => $"{scopeType}:{scopeId:N}";

    public async Task<string> JoinScopeAsync(CurrentUser currentUser, ChatScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessScopeAsync(currentUser, scopeType, scopeId, forSend: false, cancellationToken);
        await GetOrCreateThreadAsync(scopeType, scopeId, cancellationToken);
        return GetChannelName(scopeType, scopeId);
    }

    public Task<CursorPageResponse<ChatMessageDto>> ListEventMessagesAsync(Guid currentUserId, Guid eventId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUserId, ChatScopeType.Event, eventId, query, cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListGroupMessagesAsync(Guid currentUserId, Guid groupId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUserId, ChatScopeType.Group, groupId, query, cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListDirectMessagesAsync(Guid currentUserId, Guid directChatId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUserId, ChatScopeType.Direct, directChatId, query, cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListMySupportMessagesAsync(CurrentUser currentUser, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(currentUser, ChatScopeType.Support, currentUser.UserId, query, cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListSupportMessagesForUserAsync(CurrentUser admin, Guid userId, ChatHistoryQuery query, CancellationToken cancellationToken = default) =>
        ListMessagesAsync(admin, ChatScopeType.Support, userId, query, cancellationToken);

    public async Task<IReadOnlyCollection<SupportThreadDto>> ListSupportThreadsAsync(CurrentUser admin, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(admin);

        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);
        var threads = await messagingRepository.ListThreadsByScopeTypeAsync(ChatScopeType.Support, cancellationToken);
        var results = new List<SupportThreadDto>(threads.Count);

        foreach (var thread in threads)
        {
            if (!accounts.TryGetValue(thread.ScopeId, out var account))
            {
                continue;
            }

            var messages = (await messagingRepository.ListMessagesAsync(thread.Id, cancellationToken))
                .OrderBy(message => message.CreatedAtUtc)
                .ThenBy(message => message.Id)
                .ToArray();
            var lastMessage = messages.LastOrDefault();

            results.Add(new SupportThreadDto(
                account.Id,
                account.Username,
                profiles.GetValueOrDefault(account.Id)?.DisplayName ?? account.Username,
                thread.CreatedAtUtc,
                lastMessage?.CreatedAtUtc,
                lastMessage?.Body,
                messages.Length));
        }

        return results
            .OrderByDescending(thread => thread.LastMessageAtUtc ?? thread.CreatedAtUtc)
            .ThenBy(thread => thread.Username, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<DirectChatDto> CreateDirectChatAsync(CurrentUser currentUser, CreateDirectChatRequest request, CancellationToken cancellationToken = default)
    {
        EnsureScopeIsLaunched(ChatScopeType.Direct);

        var subjectUserId = request.SubjectUserId ?? throw ApiException.BadRequest("subjectUserId is required.");

        if (subjectUserId == currentUser.UserId)
        {
            throw ApiException.BadRequest("You cannot start a direct chat with yourself.");
        }

        var connection = await discoveryRepository.GetBudConnectionAsync(currentUser.UserId, subjectUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested direct chat could not be found.");

        if (connection.State != BudConnectionState.Connected)
        {
            throw ApiException.NotFound("The requested direct chat could not be found.");
        }

        await EnsureDirectChatIsVisibleAsync(currentUser.UserId, connection, cancellationToken);
        await GetOrCreateThreadAsync(ChatScopeType.Direct, connection.Id, cancellationToken);

        return await MapDirectChatAsync(currentUser.UserId, connection, cancellationToken);
    }

    public async Task<ChatMessageDto> SendAsync(CurrentUser currentUser, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var scopeType = request.ScopeType ?? throw ApiException.BadRequest("scopeType is required.");
        var scopeId = request.ScopeId ?? throw ApiException.BadRequest("scopeId is required.");
        var body = string.IsNullOrWhiteSpace(request.Body)
            ? throw ApiException.BadRequest("body is required.")
            : request.Body.Trim();

        await EnsureCanAccessScopeAsync(currentUser, scopeType, scopeId, forSend: true, cancellationToken);

        var thread = await GetOrCreateThreadAsync(scopeType, scopeId, cancellationToken);
        var message = new ChatMessage(Guid.NewGuid(), thread.Id, currentUser.UserId, body, clock.UtcNow);
        await messagingRepository.SaveMessageAsync(message, cancellationToken);

        return await MapMessageAsync(message, cancellationToken);
    }

    public Task<ChatMessageDto> SendMySupportMessageAsync(CurrentUser currentUser, SendSupportMessageRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(
            currentUser,
            new SendChatMessageRequest
            {
                ScopeType = ChatScopeType.Support,
                ScopeId = currentUser.UserId,
                Body = request.Body,
            },
            cancellationToken);

    public Task<ChatMessageDto> SendSupportMessageForUserAsync(CurrentUser admin, Guid userId, SendSupportMessageRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(
            admin,
            new SendChatMessageRequest
            {
                ScopeType = ChatScopeType.Support,
                ScopeId = userId,
                Body = request.Body,
            },
            cancellationToken);

    public Task<ChatMessageDto> SendDirectMessageAsync(CurrentUser currentUser, Guid directChatId, SendDirectChatMessageRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(
            currentUser,
            new SendChatMessageRequest
            {
                ScopeType = ChatScopeType.Direct,
                ScopeId = directChatId,
                Body = request.Body,
            },
            cancellationToken);

    private async Task<CursorPageResponse<ChatMessageDto>> ListMessagesAsync(
        Guid currentUserId,
        ChatScopeType scopeType,
        Guid scopeId,
        ChatHistoryQuery query,
        CancellationToken cancellationToken) =>
        await ListMessagesAsync(new CurrentUser(currentUserId, string.Empty, Array.Empty<UserRole>()), scopeType, scopeId, query, cancellationToken);

    private async Task<CursorPageResponse<ChatMessageDto>> ListMessagesAsync(
        CurrentUser currentUser,
        ChatScopeType scopeType,
        Guid scopeId,
        ChatHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureCanAccessScopeAsync(currentUser, scopeType, scopeId, forSend: false, cancellationToken);
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
        EnsureScopeIsLaunched(scopeType);

        var existing = await messagingRepository.GetThreadByScopeAsync(scopeType, scopeId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = new ChatThread(Guid.NewGuid(), scopeType, scopeId, clock.UtcNow);
        await messagingRepository.SaveThreadAsync(created, cancellationToken);
        return await messagingRepository.GetThreadByScopeAsync(scopeType, scopeId, cancellationToken) ?? created;
    }

    private async Task EnsureCanAccessScopeAsync(CurrentUser currentUser, ChatScopeType scopeType, Guid scopeId, bool forSend, CancellationToken cancellationToken)
    {
        EnsureScopeIsLaunched(scopeType);
        var currentUserId = currentUser.UserId;

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

            case ChatScopeType.Direct:
                var connection = await discoveryRepository.GetBudConnectionByIdAsync(scopeId, cancellationToken)
                    ?? throw ApiException.NotFound("The requested chat scope could not be found.");

                if (connection.State != BudConnectionState.Connected ||
                    (connection.UserOneId != currentUserId && connection.UserTwoId != currentUserId))
                {
                    throw ApiException.NotFound("The requested chat scope could not be found.");
                }

                await EnsureDirectChatIsVisibleAsync(currentUserId, connection, cancellationToken);
                break;

            case ChatScopeType.Support:
                var supportedAccount = await authRepository.GetByIdAsync(scopeId, cancellationToken)
                    ?? throw ApiException.NotFound("The requested chat scope could not be found.");

                if (supportedAccount.Status != AccountStatus.Active)
                {
                    throw ApiException.NotFound("The requested chat scope could not be found.");
                }

                if (currentUserId != scopeId && !currentUser.Roles.Contains(UserRole.Admin))
                {
                    throw ApiException.NotFound("The requested chat scope could not be found.");
                }

                break;

            default:
                throw ApiException.NotFound("The requested chat scope could not be found.");
        }

        if (forSend && !(scopeType == ChatScopeType.Support && currentUser.Roles.Contains(UserRole.Admin)))
        {
            await restrictionService.EnsureNotRestrictedAsync(currentUserId, RestrictionScope.ChatSend, "You are currently restricted from sending chat messages.", cancellationToken);
        }
    }

    private void EnsureScopeIsLaunched(ChatScopeType scopeType)
    {
        if (scopeType == ChatScopeType.Direct && !featureFlagService.IsMessagingDirectChatEnabled())
        {
            throw ApiException.NotFound("The requested chat scope could not be found.");
        }

        if (scopeType == ChatScopeType.Group && !featureFlagService.IsMessagingGroupChatEnabled())
        {
            throw ApiException.NotFound("The requested chat scope could not be found.");
        }
    }

    private static void EnsureAdmin(CurrentUser currentUser)
    {
        if (!currentUser.Roles.Contains(UserRole.Admin))
        {
            throw ApiException.Forbidden("Only admins can access support threads.");
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

    private async Task EnsureDirectChatIsVisibleAsync(Guid currentUserId, BudConnection connection, CancellationToken cancellationToken)
    {
        var otherUserId = connection.UserOneId == currentUserId ? connection.UserTwoId : connection.UserOneId;

        if (await profileRepository.GetBlockAsync(currentUserId, otherUserId, cancellationToken) is not null ||
            await profileRepository.GetBlockAsync(otherUserId, currentUserId, cancellationToken) is not null)
        {
            throw ApiException.NotFound("The requested chat scope could not be found.");
        }
    }

    private async Task<DirectChatDto> MapDirectChatAsync(Guid currentUserId, BudConnection connection, CancellationToken cancellationToken)
    {
        var otherUserId = connection.UserOneId == currentUserId ? connection.UserTwoId : connection.UserOneId;
        var account = await authRepository.GetByIdAsync(otherUserId, cancellationToken)
            ?? throw ApiException.NotFound("The requested direct chat could not be found.");
        var profile = await profileRepository.GetProfileAsync(otherUserId, cancellationToken);

        return new DirectChatDto(
            connection.Id,
            otherUserId,
            account.Username,
            profile?.DisplayName ?? account.Username,
            connection.CreatedAtUtc);
    }
}
