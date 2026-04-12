using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Web.Mvc.Services;

/// <summary>
/// Thin wrapper over chat-history endpoints plus hub metadata used by a future MVC chat UI.
/// </summary>
public sealed class MessagingApiService
{
    public const string HubPath = "/hubs/chat";
    public const string JoinScopeMethodName = "JoinScope";
    public const string SendMessageMethodName = "SendMessage";
    public const string MessageReceivedEventName = "MessageReceived";

    private readonly BackendHttpClient backendHttpClient;

    public MessagingApiService(BackendHttpClient backendHttpClient)
    {
        this.backendHttpClient = backendHttpClient;
    }

    public Task<CursorPageResponse<ChatMessageDto>> ListEventMessagesAsync(
        Guid eventId,
        ChatHistoryQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<CursorPageResponse<ChatMessageDto>>(
            BuildEventMessagesPath(eventId, query ?? new ChatHistoryQuery()),
            cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListGroupMessagesAsync(
        Guid groupId,
        ChatHistoryQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<CursorPageResponse<ChatMessageDto>>(
            BuildGroupMessagesPath(groupId, query ?? new ChatHistoryQuery()),
            cancellationToken);

    public Task<DirectChatDto> CreateDirectChatAsync(
        CreateDirectChatRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<CreateDirectChatRequest, DirectChatDto>(
            "/api/v1/direct-chats",
            request,
            cancellationToken: cancellationToken);

    public Task<CursorPageResponse<ChatMessageDto>> ListDirectMessagesAsync(
        Guid directChatId,
        ChatHistoryQuery? query = null,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.GetAsync<CursorPageResponse<ChatMessageDto>>(
            BuildDirectMessagesPath(directChatId, query ?? new ChatHistoryQuery()),
            cancellationToken);

    public Task<ChatMessageDto> SendDirectMessageAsync(
        Guid directChatId,
        SendDirectChatMessageRequest request,
        CancellationToken cancellationToken = default) =>
        backendHttpClient.PostAsync<SendDirectChatMessageRequest, ChatMessageDto>(
            $"/api/v1/direct-chats/{directChatId}/messages",
            request,
            cancellationToken: cancellationToken);

    private static string BuildEventMessagesPath(Guid eventId, ChatHistoryQuery query) =>
        $"/api/v1/events/{eventId}/messages{BuildHistoryQueryString(query)}";

    private static string BuildGroupMessagesPath(Guid groupId, ChatHistoryQuery query) =>
        $"/api/v1/groups/{groupId}/messages{BuildHistoryQueryString(query)}";

    private static string BuildDirectMessagesPath(Guid directChatId, ChatHistoryQuery query) =>
        $"/api/v1/direct-chats/{directChatId}/messages{BuildHistoryQueryString(query)}";

    private static string BuildHistoryQueryString(ChatHistoryQuery query)
    {
        var builder = new QueryBuilder();

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            builder.Add("cursor", query.Cursor);
        }

        builder.Add("pageSize", query.PageSize.ToString(CultureInfo.InvariantCulture));
        return builder.ToQueryString().ToString();
    }
}
