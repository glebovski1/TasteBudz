using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/direct-chats")]
public sealed class DirectChatsController(
    MessagingService messagingService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpPost]
    public Task<DirectChatDto> Create([FromBody] CreateDirectChatRequest request, CancellationToken cancellationToken) =>
        messagingService.CreateDirectChatAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);

    [HttpGet("{directChatId:guid}/messages")]
    public Task<CursorPageResponse<ChatMessageDto>> ListMessages(
        Guid directChatId,
        [FromQuery] ChatHistoryQuery query,
        CancellationToken cancellationToken) =>
        messagingService.ListDirectMessagesAsync(currentUserAccessor.GetRequiredCurrentUser().UserId, directChatId, query, cancellationToken);

    [HttpPost("{directChatId:guid}/messages")]
    public Task<ChatMessageDto> SendMessage(
        Guid directChatId,
        [FromBody] SendDirectChatMessageRequest request,
        CancellationToken cancellationToken) =>
        messagingService.SendDirectMessageAsync(currentUserAccessor.GetRequiredCurrentUser(), directChatId, request, cancellationToken);
}
