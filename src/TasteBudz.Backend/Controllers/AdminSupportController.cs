using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Modules.Messaging;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/support")]
public sealed class AdminSupportController(
    MessagingService messagingService,
    ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet("threads")]
    public async Task<ActionResult<IReadOnlyCollection<SupportThreadDto>>> ListThreads(CancellationToken cancellationToken)
    {
        var threads = await messagingService.ListSupportThreadsAsync(currentUserAccessor.GetRequiredCurrentUser(), cancellationToken);
        return Ok(threads);
    }

    [HttpGet("threads/{userId:guid}/messages")]
    public Task<CursorPageResponse<ChatMessageDto>> ListMessages(Guid userId, [FromQuery] ChatHistoryQuery query, CancellationToken cancellationToken) =>
        messagingService.ListSupportMessagesForUserAsync(currentUserAccessor.GetRequiredCurrentUser(), userId, query, cancellationToken);

    [HttpPost("threads/{userId:guid}/messages")]
    public Task<ChatMessageDto> SendMessage(Guid userId, [FromBody] SendSupportMessageRequest request, CancellationToken cancellationToken) =>
        messagingService.SendSupportMessageForUserAsync(currentUserAccessor.GetRequiredCurrentUser(), userId, request, cancellationToken);
}
