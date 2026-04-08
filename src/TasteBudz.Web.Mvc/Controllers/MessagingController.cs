using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TasteBudz.Backend.Modules.Messaging;
using TasteBudz.Web.Mvc.Options;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Serves the chat inbox and the shared chat panel for event and group scopes.
/// </summary>
[Authorize]
[Route("[controller]/[action]")]
public sealed class MessagingController : Controller
{
    private readonly MessagingApiService messagingApiService;
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;
    private readonly string backendBaseUrl;

    public MessagingController(
        MessagingApiService messagingApiService,
        ProfileApiService profileApiService,
        UserSessionService userSessionService,
        IOptions<BackendApiOptions> backendOptions)
    {
        this.messagingApiService = messagingApiService;
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
        this.backendBaseUrl = backendOptions.Value.BaseUrl.TrimEnd('/');
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
    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> EventChat(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListEventMessagesAsync(eventId, cancellationToken: cancellationToken);
            var model = ChatViewModel.ForEvent(eventId, history.Items);
            SetHubUrl();
            return View("Chat", model);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            SetHubUrl();
            return View("Chat", ChatViewModel.ForEvent(eventId, []));
        }
    }

    // GET /Messaging/GroupChat/{groupId}
    [HttpGet]
    [HttpGet("{groupId:guid}")]
    public async Task<IActionResult> GroupChat(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListGroupMessagesAsync(groupId, cancellationToken: cancellationToken);
            var model = ChatViewModel.ForGroup(groupId, history.Items);
            SetHubUrl();
            return View("Chat", model);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            SetHubUrl();
            return View("Chat", ChatViewModel.ForGroup(groupId, []));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Passes the full backend hub URL to the view so SignalR connects to the
    /// correct server (the backend) rather than the MVC frontend.
    /// </summary>
    private void SetHubUrl()
    {
        ViewData["HubUrl"] = $"{backendBaseUrl}{MessagingApiService.HubPath}";
        ViewData["BackendAccessToken"] = userSessionService.GetSession()?.AccessToken ?? "";
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}