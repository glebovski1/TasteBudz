using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Serves the chat inbox and the shared chat panel for event and group scopes.
/// </summary>
[Authorize]
public sealed class MessagingController : Controller
{
    private readonly MessagingApiService messagingApiService;
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;

    public MessagingController(
        MessagingApiService messagingApiService,
        ProfileApiService profileApiService,
        UserSessionService userSessionService)
    {
        this.messagingApiService = messagingApiService;
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Messaging/Chat
    [HttpGet]
    public async Task<IActionResult> Chat(CancellationToken cancellationToken)
    {
        try
        {
            var events = await profileApiService.ListMyEventsAsync(cancellationToken);
            var groups = await profileApiService.ListMyGroupsAsync(cancellationToken);
            return View("Inbox", ChatInboxViewModel.FromDtos(events, groups));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load your chats.";
            return View("Inbox", ChatInboxViewModel.Empty);
        }
    }

    // GET /Messaging/EventChat/{eventId}
    [HttpGet]
    public async Task<IActionResult> EventChat(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListEventMessagesAsync(eventId, cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForEvent(eventId, history.Items));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            return View("Chat", ChatViewModel.ForEvent(eventId, []));
        }
    }

    // GET /Messaging/GroupChat/{groupId}
    [HttpGet]
    public async Task<IActionResult> GroupChat(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListGroupMessagesAsync(groupId, cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForGroup(groupId, history.Items));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            return View("Chat", ChatViewModel.ForGroup(groupId, []));
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}