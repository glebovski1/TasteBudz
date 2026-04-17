using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/support")]
public sealed class SupportController(
    MessagingService messagingService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("messages")]
    public Task<CursorPageResponse<ChatMessageDto>> ListMessages([FromQuery] ChatHistoryQuery query, CancellationToken cancellationToken) =>
        messagingService.ListMySupportMessagesAsync(currentUserAccessor.GetRequiredCurrentUser(), query, cancellationToken);

    [HttpPost("messages")]
    public Task<ChatMessageDto> SendMessage([FromBody] SendSupportMessageRequest request, CancellationToken cancellationToken) =>
        messagingService.SendMySupportMessageAsync(currentUserAccessor.GetRequiredCurrentUser(), request, cancellationToken);
}
