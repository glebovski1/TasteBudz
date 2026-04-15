using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly NotificationApiService notificationApiService;
    private readonly UserSessionService userSessionService;

    public NotificationsController(
        NotificationApiService notificationApiService,
        UserSessionService userSessionService)
    {
        this.notificationApiService = notificationApiService;
        this.userSessionService = userSessionService;
    }

    // GET /Notifications/Index — full notifications page
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await notificationApiService.ListAsync(cancellationToken);
            return View(NotificationsViewModel.FromDtos(notifications));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            return View(NotificationsViewModel.Empty);
        }
    }

    // GET /Notifications/Summary — returns JSON for the nav bell (unread count + recent items)
    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await notificationApiService.ListAsync(cancellationToken);
            var unread = notifications.Where(n => n.ReadAtUtc is null).ToList();
            var recent = notifications
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(8)
                .Select(n => new
                {
                    id = n.NotificationId,
                    message = n.Message,
                    type = n.NotificationType.ToString(),
                    contextType = n.ContextType,
                    contextId = n.ContextId,
                    createdAt = n.CreatedAtUtc,
                    isRead = n.ReadAtUtc is not null,
                    link = NotificationsViewModel.BuildLink(n.ContextType, n.ContextId),
                })
                .ToList();

            return Ok(new { unreadCount = unread.Count, items = recent });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return Unauthorized();
        }
        catch
        {
            return Ok(new { unreadCount = 0, items = Array.Empty<object>() });
        }
    }

    // POST /Notifications/MarkRead/{id} — marks a single notification read, returns JSON
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await notificationApiService.UpdateAsync(
                id,
                new UpdateNotificationRequest { Read = true },
                cancellationToken);

            return Ok(new { success = true });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return Unauthorized();
        }
        catch
        {
            return BadRequest(new { success = false });
        }
    }

    // POST /Notifications/MarkAllRead — marks all unread notifications read
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await notificationApiService.ListAsync(cancellationToken);
            var unread = notifications.Where(n => n.ReadAtUtc is null).ToList();

            foreach (var n in unread)
            {
                await notificationApiService.UpdateAsync(
                    n.NotificationId,
                    new UpdateNotificationRequest { Read = true },
                    cancellationToken);
            }

            TempData["StatusMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "Could not mark notifications as read.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}