// SignalR hub for event and group chat delivery.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;

namespace TasteBudz.Backend.Modules.Messaging;

/// <summary>
/// Authenticated real-time hub for scope-based event and group chat.
/// </summary>
[Authorize]
public sealed class ChatHub(MessagingService messagingService) : Hub
{
    public async Task JoinScope(ChatScopeType scopeType, Guid scopeId)
    {
        try
        {
            var channel = await messagingService.JoinScopeAsync(GetCurrentUser(), scopeType, scopeId, Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, channel, Context.ConnectionAborted);
        }
        catch (ApiException exception)
        {
            throw new HubException(exception.Detail);
        }
    }

    public async Task<ChatMessageDto> SendMessage(SendChatMessageRequest request)
    {
        try
        {
            var message = await messagingService.SendAsync(GetCurrentUser(), request, Context.ConnectionAborted);
            await Clients.Group(MessagingService.GetChannelName(request.ScopeType!.Value, request.ScopeId!.Value))
                .SendAsync("MessageReceived", message, cancellationToken: Context.ConnectionAborted);
            return message;
        }
        catch (ApiException exception)
        {
            throw new HubException(exception.Detail);
        }
    }

    private CurrentUser GetCurrentUser()
    {
        var principal = Context.User;
        var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal?.FindFirstValue(ClaimTypes.Name);

        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(username))
        {
            throw new HubException("Authentication is required for this operation.");
        }

        var roles = principal!.FindAll(ClaimTypes.Role)
            .Select(claim => Enum.TryParse<UserRole>(claim.Value, true, out var role) ? (UserRole?)role : null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .ToArray();

        return new CurrentUser(userId, username, roles);
    }
}
