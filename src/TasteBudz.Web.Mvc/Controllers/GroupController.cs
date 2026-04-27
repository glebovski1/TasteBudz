using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Web.Mvc.Services;
using TasteBudz.Web.Mvc.ViewModels;

namespace TasteBudz.Web.Mvc.Controllers;

/// <summary>
/// Handles the Groups area: browse, create, detail/manage, join, leave,
/// member removal, and (for private groups) invite workflows.
/// </summary>
[Authorize]
public sealed class GroupController : Controller
{
    private readonly GroupApiService groupApiService;
    private readonly EventApiService eventApiService;
    private readonly ProfileApiService profileApiService;
    private readonly UserSessionService userSessionService;

    public GroupController(
        GroupApiService groupApiService,
        EventApiService eventApiService,
        ProfileApiService profileApiService,
        UserSessionService userSessionService)
    {
        this.groupApiService = groupApiService;
        this.eventApiService = eventApiService;
        this.profileApiService = profileApiService;
        this.userSessionService = userSessionService;
    }

    // ── GET /Group/Index ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DashboardGroupSummaryDto> myGroups;

        try
        {
            myGroups = await TryListMyGroupsAsync(cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }

        try
        {
            var allGroups = await FetchAllGroupsAsync(q, cancellationToken);
            var allGroupsCount = allGroups.Count;

            return View(GroupIndexViewModel.FromDto(allGroups, allGroupsCount, q, myGroups));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            return View(GroupIndexViewModel.FromDto(Array.Empty<GroupSummaryDto>(), 0, q, myGroups));
        }
    }

    // ── GET /Group/Create ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult CreateGroup() => View(new GroupCreateViewModel());

    // ── POST /Group/Create ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(GroupCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var detail = await groupApiService.CreateAsync(model.ToRequest(), cancellationToken);
            TempData["StatusMessage"] = $"'{detail.Name}' was created successfully.";
            return RedirectToAction(nameof(Manage), new { groupId = detail.GroupId });
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ── GET /Group/Manage/{groupId} ──────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Manage(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await groupApiService.GetAsync(groupId, cancellationToken);
            var currentUserId = GetCurrentUserId();
            var linkedEvents = await TryListGroupEventsAsync(groupId, cancellationToken);
            var eventHistory = await BuildEventHistoryAsync(linkedEvents, currentUserId, cancellationToken);
            var announcements = await TryListAnnouncementsAsync(groupId, cancellationToken);

            return View(GroupManageViewModel.FromDto(detail, currentUserId, eventHistory, announcements));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            TempData["StatusMessage"] = "That group could not be found.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── POST /Group/UpdateSettings ───────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(Guid groupId, GroupManageViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await groupApiService.UpdateAsync(groupId, new UpdateGroupRequest
            {
                Name = model.EditName,
                Description = string.IsNullOrWhiteSpace(model.EditDescription) ? null : model.EditDescription,
                Visibility = model.EditVisibility,
                WallpaperTheme = model.EditWallpaperTheme,
            }, cancellationToken);

            TempData["StatusMessage"] = "Group settings updated.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(Guid groupId, GroupManageViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.AnnouncementTitle) || string.IsNullOrWhiteSpace(model.AnnouncementBody))
        {
            TempData["StatusMessage"] = "Please enter an announcement title and message.";
            return RedirectToAction(nameof(Manage), new { groupId });
        }

        try
        {
            await groupApiService.CreateAnnouncementAsync(groupId, new CreateGroupAnnouncementRequest
            {
                Title = model.AnnouncementTitle,
                Body = model.AnnouncementBody,
            }, cancellationToken);

            TempData["StatusMessage"] = "Announcement posted.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not post announcement: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { groupId });
    }

    // ── POST /Group/Join ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            await groupApiService.JoinAsync(groupId, cancellationToken);
            TempData["StatusMessage"] = "You joined the group!";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not join: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { groupId });
    }

    // ── POST /Group/Leave ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            await groupApiService.LeaveAsync(groupId, cancellationToken);
            TempData["StatusMessage"] = "You left the group.";
            return RedirectToAction(nameof(Index));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not leave: {ex.Message}";
            return RedirectToAction(nameof(Manage), new { groupId });
        }
    }

    // ── POST /Group/RemoveMember ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await groupApiService.RemoveMemberAsync(groupId, userId, cancellationToken);
            TempData["StatusMessage"] = "Member removed.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not remove member: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { groupId });
    }

    // ── POST /Group/Invite ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(Guid groupId, GroupManageViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.InviteUsername))
        {
            TempData["StatusMessage"] = "Please enter a username to invite.";
            return RedirectToAction(nameof(Manage), new { groupId });
        }

        try
        {
            await groupApiService.InviteAsync(groupId,
                new InviteUserToGroupRequest { Username = model.InviteUsername.Trim() },
                cancellationToken);

            TempData["StatusMessage"] = $"{model.InviteUsername} was invited to the group.";
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch (BackendApiException ex)
        {
            TempData["StatusMessage"] = $"Could not invite: {ex.Message}";
        }

        return RedirectToAction(nameof(Manage), new { groupId });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<IReadOnlyList<GroupSummaryDto>> FetchAllGroupsAsync(string? searchQuery, CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var page = 1;
        var totalCount = 0;
        var allGroups = new List<GroupSummaryDto>();

        do
        {
            var result = await groupApiService.BrowseAsync(
                new BrowseGroupsQuery { Q = searchQuery, Page = page, PageSize = pageSize },
                cancellationToken);

            allGroups.AddRange(result.Items);
            totalCount = result.TotalCount;
            page++;
        }
        while (allGroups.Count < totalCount && allGroups.Count > 0);

        return allGroups;
    }

    private async Task<IReadOnlyCollection<DashboardGroupSummaryDto>> TryListMyGroupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await profileApiService.ListMyGroupsAsync(cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<EventSummaryDto>> TryListGroupEventsAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await groupApiService.ListGroupEventsAsync(
                groupId,
                new GroupEventsQuery { PageSize = 50 },
                cancellationToken);

            return result.Items;
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<GroupEventHistoryItem>> BuildEventHistoryAsync(
        IReadOnlyCollection<EventSummaryDto> events,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var items = new List<GroupEventHistoryItem>(events.Count);

        foreach (var eventSummary in events.OrderByDescending(item => item.EventStartAtUtc))
        {
            IReadOnlyCollection<EventFeedbackDto> feedback = eventSummary.Status == EventStatus.Completed
                ? await TryListEventFeedbackAsync(eventSummary.EventId, cancellationToken)
                : Array.Empty<EventFeedbackDto>();

            items.Add(GroupEventHistoryItem.FromDto(eventSummary, feedback, currentUserId));
        }

        return items;
    }

    private async Task<IReadOnlyList<GroupAnnouncementItem>> TryListAnnouncementsAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await groupApiService.ListAnnouncementsAsync(groupId, cancellationToken);
            return result.Items.Select(GroupAnnouncementItem.FromDto).ToList();
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException)
        {
            return [];
        }
    }

    private async Task<IReadOnlyCollection<EventFeedbackDto>> TryListEventFeedbackAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await eventApiService.ListFeedbackAsync(eventId, cancellationToken);
        }
        catch (BackendAuthenticationExpiredException)
        {
            throw;
        }
        catch (BackendApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
