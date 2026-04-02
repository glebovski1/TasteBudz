using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Modules.Groups;
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
    private readonly UserSessionService userSessionService;

    public GroupController(GroupApiService groupApiService, UserSessionService userSessionService)
    {
        this.groupApiService = groupApiService;
        this.userSessionService = userSessionService;
    }

    // ── GET /Group/Index ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        try
        {
            var result = await groupApiService.BrowseAsync(
                new BrowseGroupsQuery { Q = q, PageSize = 20 },
                cancellationToken);

            return View(GroupIndexViewModel.FromDto(result.Items, q));
        }
        catch (BackendAuthenticationExpiredException)
        {
            return await RedirectToLoginAsync(cancellationToken);
        }
        catch
        {
            return View(GroupIndexViewModel.Empty);
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
            return View(GroupManageViewModel.FromDto(detail, currentUserId));
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

    private async Task<IActionResult> RedirectToLoginAsync(CancellationToken cancellationToken)
    {
        await userSessionService.SignOutAsync(cancellationToken);
        return RedirectToAction(nameof(AccountController.Login), "Account");
    }
}
