using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IBackendApiBaseAddressProvider backendApiBaseAddressProvider;

    public MessagingController(
        MessagingApiService messagingApiService,
        ProfileApiService profileApiService,
        UserSessionService userSessionService,
        IBackendApiBaseAddressProvider backendApiBaseAddressProvider)
    {
        this.messagingApiService = messagingApiService;
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
        this.backendApiBaseAddressProvider = backendApiBaseAddressProvider;
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
            return View("Chat", ChatViewModel.ForEvent(eventId, history.Items, BuildHubUrl(), GetAccessToken()));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            return View("Chat", ChatViewModel.ForEvent(eventId, [], BuildHubUrl(), GetAccessToken()));
        }
    }

    // GET /Messaging/GroupChat/{groupId}
    [HttpGet]
    public async Task<IActionResult> GroupChat(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListGroupMessagesAsync(groupId, cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForGroup(groupId, history.Items, BuildHubUrl(), GetAccessToken()));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            return View("Chat", ChatViewModel.ForGroup(groupId, [], BuildHubUrl(), GetAccessToken()));
        }
    }

    // GET /Messaging/DirectChat/{directChatId}
    [HttpGet]
    public async Task<IActionResult> DirectChat(Guid directChatId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListDirectMessagesAsync(directChatId, cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForDirect(directChatId, history.Items, BuildHubUrl(), GetAccessToken()));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load chat history.";
            return View("Chat", ChatViewModel.ForDirect(directChatId, [], BuildHubUrl(), GetAccessToken()));
        }
    }

    // GET /Messaging/Support
    [HttpGet]
    public async Task<IActionResult> Support(CancellationToken cancellationToken)
    {
        var session = userSessionService.GetRequiredSession();

        try
        {
            var history = await messagingApiService.ListSupportMessagesAsync(cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForSupport(session.CurrentUser.UserId, history.Items, BuildHubUrl(), GetAccessToken()));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load support messages.";
            return View("Chat", ChatViewModel.ForSupport(session.CurrentUser.UserId, [], BuildHubUrl(), GetAccessToken()));
        }
    }

    // GET /Messaging/AdminSupportThread/{userId}
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminSupportThread(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await messagingApiService.ListAdminSupportMessagesAsync(userId, cancellationToken: cancellationToken);
            return View("Chat", ChatViewModel.ForSupport(userId, history.Items, BuildHubUrl(), GetAccessToken()));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not load support messages.";
            return View("Chat", ChatViewModel.ForSupport(userId, [], BuildHubUrl(), GetAccessToken()));
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }

    private string BuildHubUrl() =>
        new Uri(backendApiBaseAddressProvider.GetBaseAddress(), MessagingApiService.HubPath).ToString();

    private string GetAccessToken() => userSessionService.GetSession()?.AccessToken ?? string.Empty;
}
