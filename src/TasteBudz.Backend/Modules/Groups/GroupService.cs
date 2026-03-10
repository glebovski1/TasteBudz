// Group browse, ownership, membership, and invite workflows.
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// Owns persistent-group workflows for the MVP owner/member model.
/// </summary>
public sealed class GroupService(
    IGroupRepository groupRepository,
    IEventRepository eventRepository,
    IAuthRepository authRepository,
    IProfileRepository profileRepository,
    INotificationService notificationService,
    EventLifecycleService eventLifecycleService,
    IClock clock)
{
    public async Task<ListResponse<GroupSummaryDto>> BrowseAsync(BrowseGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var groups = await groupRepository.ListAsync(cancellationToken);
        var filtered = new List<GroupSummaryDto>();

        foreach (var group in groups)
        {
            if (group.LifecycleState != GroupLifecycleState.Active || group.Visibility != GroupVisibility.Public)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.Q) &&
                !group.Name.Contains(query.Q.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var members = await groupRepository.ListMembersAsync(group.Id, cancellationToken);
            filtered.Add(new GroupSummaryDto(
                group.Id,
                group.Name,
                group.Description,
                group.Visibility,
                members.Count(member => member.State == GroupMemberState.Active)));
        }

        var ordered = filtered
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var items = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<GroupSummaryDto>(items, ordered.Length);
    }

    public async Task<GroupDetailDto> CreateAsync(CurrentUser currentUser, CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var visibility = request.Visibility ?? throw ApiException.BadRequest("visibility is required.");
        var name = NormalizeRequiredName(request.Name);
        var description = NormalizeOptional(request.Description);
        var groupId = Guid.NewGuid();
        var group = new Group(groupId, currentUser.UserId, name, description, visibility, GroupLifecycleState.Active, now, now);
        var membership = new GroupMember(groupId, currentUser.UserId, GroupMemberState.Active, now, now);

        await groupRepository.SaveAsync(group, cancellationToken);
        await groupRepository.SaveMemberAsync(membership, cancellationToken);

        return await MapDetailAsync(currentUser.UserId, group, cancellationToken);
    }

    public async Task<GroupDetailDto> GetAsync(Guid currentUserId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);
        await EnsureCanViewAsync(currentUserId, group, cancellationToken);
        return await MapDetailAsync(currentUserId, group, cancellationToken);
    }

    public async Task<GroupDetailDto> UpdateAsync(CurrentUser currentUser, Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);
        EnsureOwner(currentUser.UserId, group);

        var updated = group with
        {
            Name = request.Name is null ? group.Name : NormalizeRequiredName(request.Name),
            Description = request.Description is null ? group.Description : NormalizeOptional(request.Description),
            Visibility = request.Visibility ?? group.Visibility,
            UpdatedAtUtc = clock.UtcNow,
        };

        await groupRepository.SaveAsync(updated, cancellationToken);
        return await MapDetailAsync(currentUser.UserId, updated, cancellationToken);
    }

    public async Task<ListResponse<EventSummaryDto>> ListGroupEventsAsync(
        Guid currentUserId,
        Guid groupId,
        GroupEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);
        await EnsureCanViewAsync(currentUserId, group, cancellationToken);

        var events = await eventRepository.ListAsync(cancellationToken);
        var items = new List<EventSummaryDto>();

        foreach (var eventRecord in events.Where(candidate => candidate.GroupId == groupId))
        {
            var synchronized = await eventLifecycleService.SynchronizeAsync(eventRecord, cancellationToken);

            if (!await CanViewLinkedEventAsync(currentUserId, synchronized, cancellationToken))
            {
                continue;
            }

            var participants = await eventRepository.ListParticipantsAsync(synchronized.Id, cancellationToken);
            items.Add(EventDtoMapper.ToSummary(synchronized, participants.Count(participant => participant.State == EventParticipantState.Joined)));
        }

        var ordered = items
            .OrderBy(item => item.EventStartAtUtc)
            .ToArray();
        var pageItems = ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new ListResponse<EventSummaryDto>(pageItems, ordered.Length);
    }

    public async Task<GroupDetailDto> JoinAsync(Guid currentUserId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);

        if (group.Visibility != GroupVisibility.Public)
        {
            throw ApiException.Conflict("Private groups require an invitation in the MVP.");
        }

        var existing = await groupRepository.GetMemberAsync(groupId, currentUserId, cancellationToken);

        if (existing?.State == GroupMemberState.Active)
        {
            throw ApiException.Conflict("You are already a member of this group.");
        }

        if (existing?.State == GroupMemberState.Removed)
        {
            throw ApiException.Forbidden("You have been removed from this group.");
        }

        var now = clock.UtcNow;
        var membership = new GroupMember(groupId, currentUserId, GroupMemberState.Active, existing?.CreatedAtUtc ?? now, now);
        await groupRepository.SaveMemberAsync(membership, cancellationToken);

        return await MapDetailAsync(currentUserId, group, cancellationToken);
    }

    public async Task LeaveAsync(Guid currentUserId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);

        if (group.OwnerUserId == currentUserId)
        {
            throw ApiException.Conflict("The group owner cannot leave while ownership transfer remains out of scope.");
        }

        var membership = await groupRepository.GetMemberAsync(groupId, currentUserId, cancellationToken)
            ?? throw ApiException.Conflict("You are not an active member of this group.");

        if (membership.State != GroupMemberState.Active)
        {
            throw ApiException.Conflict("You are not an active member of this group.");
        }

        await groupRepository.SaveMemberAsync(membership with
        {
            State = GroupMemberState.Left,
            UpdatedAtUtc = clock.UtcNow,
        }, cancellationToken);
    }

    public async Task RemoveMemberAsync(CurrentUser currentUser, Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);
        EnsureOwner(currentUser.UserId, group);

        if (userId == group.OwnerUserId)
        {
            throw ApiException.BadRequest("The group owner cannot be removed.");
        }

        var membership = await groupRepository.GetMemberAsync(groupId, userId, cancellationToken)
            ?? throw ApiException.NotFound("The requested group member could not be found.");

        if (membership.State == GroupMemberState.Removed)
        {
            return;
        }

        await groupRepository.SaveMemberAsync(membership with
        {
            State = GroupMemberState.Removed,
            UpdatedAtUtc = clock.UtcNow,
        }, cancellationToken);
    }

    public async Task<GroupInviteDto> InviteAsync(CurrentUser currentUser, Guid groupId, InviteUserToGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupAsync(groupId, cancellationToken);
        EnsureOwner(currentUser.UserId, group);

        if (group.Visibility != GroupVisibility.Private)
        {
            throw ApiException.Conflict("Only private groups use the invite workflow in the MVP.");
        }

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? throw ApiException.BadRequest("username is required.")
            : request.Username.Trim();
        var invitee = await authRepository.FindByUsernameAsync(username, cancellationToken)
            ?? throw ApiException.NotFound($"User '{username}' could not be found.");

        if (invitee.Id == currentUser.UserId)
        {
            throw ApiException.BadRequest("You cannot invite yourself.");
        }

        await EnsureNotBlockedAsync(currentUser.UserId, invitee.Id, cancellationToken);

        var membership = await groupRepository.GetMemberAsync(groupId, invitee.Id, cancellationToken);

        if (membership?.State == GroupMemberState.Active)
        {
            throw ApiException.Conflict($"User '{invitee.Username}' is already a member of this group.");
        }

        var invites = await groupRepository.ListInvitesForGroupAsync(groupId, cancellationToken);
        var pending = invites.FirstOrDefault(existing =>
            existing.InvitedUserId == invitee.Id &&
            existing.Status == GroupInviteStatus.Pending);

        if (pending is not null)
        {
            throw ApiException.Conflict($"User '{invitee.Username}' already has a pending invite.");
        }

        var now = clock.UtcNow;
        var invite = new GroupInvite(Guid.NewGuid(), groupId, invitee.Id, currentUser.UserId, GroupInviteStatus.Pending, now, now);
        await groupRepository.SaveInviteAsync(invite, cancellationToken);
        await notificationService.CreateAsync(
            new Notification(
                Guid.NewGuid(),
                invitee.Id,
                NotificationType.GroupInviteReceived,
                "Group",
                groupId,
                $"You were invited to join {group.Name}.",
                now,
                null),
            cancellationToken);

        return new GroupInviteDto(invite.Id, invite.GroupId, invitee.Id, invitee.Username, invite.Status, invite.CreatedAtUtc, invite.UpdatedAtUtc);
    }

    public async Task<GroupInviteDto> RespondToInviteAsync(CurrentUser currentUser, Guid inviteId, RespondToGroupInviteRequest request, CancellationToken cancellationToken = default)
    {
        var invite = await groupRepository.GetInviteAsync(inviteId, cancellationToken)
            ?? throw ApiException.NotFound("The requested invite could not be found.");
        var group = await GetActiveGroupAsync(invite.GroupId, cancellationToken);

        if (invite.InvitedUserId != currentUser.UserId)
        {
            throw ApiException.NotFound("The requested invite could not be found.");
        }

        if (invite.Status != GroupInviteStatus.Pending)
        {
            throw ApiException.Conflict("Only pending invites can be updated.");
        }

        var status = request.Status ?? throw ApiException.BadRequest("status is required.");

        if (status is not GroupInviteStatus.Accepted and not GroupInviteStatus.Declined)
        {
            throw ApiException.BadRequest("Only Accepted or Declined are supported.");
        }

        var now = clock.UtcNow;
        var updatedInvite = invite with
        {
            Status = status,
            UpdatedAtUtc = now,
        };

        if (status == GroupInviteStatus.Accepted)
        {
            await EnsureNotBlockedAsync(group.OwnerUserId, currentUser.UserId, cancellationToken);
            var membership = await groupRepository.GetMemberAsync(group.Id, currentUser.UserId, cancellationToken);
            await groupRepository.SaveMemberAsync(new GroupMember(
                group.Id,
                currentUser.UserId,
                GroupMemberState.Active,
                membership?.CreatedAtUtc ?? now,
                now), cancellationToken);
        }

        await groupRepository.SaveInviteAsync(updatedInvite, cancellationToken);

        var account = await authRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw ApiException.NotFound("The invited user could not be found.");
        return new GroupInviteDto(updatedInvite.Id, updatedInvite.GroupId, updatedInvite.InvitedUserId, account.Username, updatedInvite.Status, updatedInvite.CreatedAtUtc, updatedInvite.UpdatedAtUtc);
    }

    private async Task<Group> GetActiveGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetAsync(groupId, cancellationToken)
            ?? throw ApiException.NotFound("The requested group could not be found.");

        if (group.LifecycleState != GroupLifecycleState.Active)
        {
            throw ApiException.NotFound("The requested group could not be found.");
        }

        return group;
    }

    private async Task EnsureCanViewAsync(Guid currentUserId, Group group, CancellationToken cancellationToken)
    {
        if (group.Visibility == GroupVisibility.Public)
        {
            return;
        }

        var membership = await groupRepository.GetMemberAsync(group.Id, currentUserId, cancellationToken);

        if (membership?.State != GroupMemberState.Active)
        {
            throw ApiException.NotFound("The requested group could not be found.");
        }
    }

    private static void EnsureOwner(Guid currentUserId, Group group)
    {
        if (group.OwnerUserId != currentUserId)
        {
            throw ApiException.Forbidden("Only the group owner can perform this action.");
        }
    }

    private async Task<GroupDetailDto> MapDetailAsync(Guid currentUserId, Group group, CancellationToken cancellationToken)
    {
        var members = await groupRepository.ListMembersAsync(group.Id, cancellationToken);
        var activeMembers = members
            .Where(member => member.State == GroupMemberState.Active)
            .ToArray();
        var accounts = (await authRepository.ListActiveAccountsAsync(cancellationToken)).ToDictionary(account => account.Id);
        var profiles = (await profileRepository.ListProfilesAsync(cancellationToken)).ToDictionary(profile => profile.UserId);
        var memberDtos = activeMembers
            .Where(member => accounts.ContainsKey(member.UserId))
            .OrderBy(member => profiles.GetValueOrDefault(member.UserId)?.DisplayName ?? accounts[member.UserId].Username, StringComparer.OrdinalIgnoreCase)
            .Select(member => new GroupMemberDto(
                member.UserId,
                accounts[member.UserId].Username,
                profiles.GetValueOrDefault(member.UserId)?.DisplayName ?? accounts[member.UserId].Username,
                member.State,
                member.CreatedAtUtc))
            .ToArray();
        var currentMembership = activeMembers.FirstOrDefault(member => member.UserId == currentUserId);

        return new GroupDetailDto(
            group.Id,
            group.OwnerUserId,
            group.Name,
            group.Description,
            group.Visibility,
            group.LifecycleState,
            currentMembership?.State == GroupMemberState.Active,
            memberDtos);
    }

    private async Task<bool> CanViewLinkedEventAsync(Guid currentUserId, Event eventRecord, CancellationToken cancellationToken)
    {
        if (eventRecord.EventType == EventType.Open || eventRecord.HostUserId == currentUserId)
        {
            return true;
        }

        var participant = await eventRepository.GetParticipantAsync(eventRecord.Id, currentUserId, cancellationToken);
        return participant is not null && participant.State != EventParticipantState.Removed;
    }

    private async Task EnsureNotBlockedAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken)
    {
        if (await profileRepository.GetBlockAsync(firstUserId, secondUserId, cancellationToken) is not null ||
            await profileRepository.GetBlockAsync(secondUserId, firstUserId, cancellationToken) is not null)
        {
            throw ApiException.Forbidden("Blocking prevents group invitations between these users.");
        }
    }

    private static string NormalizeRequiredName(string value)
    {
        var normalized = value.Trim();

        if (normalized.Length < 3)
        {
            throw ApiException.BadRequest("name must be at least 3 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
