// In-memory group repository used by the current MVP implementation.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// Stores groups, memberships, and invites inside the shared process-local store.
/// </summary>
public sealed class InMemoryGroupRepository(InMemoryTasteBudzStore store) : IGroupRepository
{
    public Task<Group?> GetAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Groups.TryGetValue(groupId, out var group);
            return Task.FromResult(group);
        }
    }

    public Task<IReadOnlyCollection<Group>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.Groups.Values
                .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Group>>(items);
        }
    }

    public Task SaveAsync(Group group, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.Groups[group.Id] = group;
            return Task.CompletedTask;
        }
    }

    public Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.GroupMembers.TryGetValue(ToMemberKey(groupId, userId), out var member);
            return Task.FromResult(member);
        }
    }

    public Task<IReadOnlyCollection<GroupMember>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.GroupMembers.Values
                .Where(member => member.GroupId == groupId)
                .OrderBy(member => member.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<GroupMember>>(items);
        }
    }

    public Task<IReadOnlyCollection<GroupMember>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.GroupMembers.Values
                .Where(member => member.UserId == userId)
                .OrderByDescending(member => member.UpdatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<GroupMember>>(items);
        }
    }

    public Task SaveMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.GroupMembers[ToMemberKey(member.GroupId, member.UserId)] = member;
            return Task.CompletedTask;
        }
    }

    public Task<GroupInvite?> GetInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.GroupInvites.TryGetValue(inviteId, out var invite);
            return Task.FromResult(invite);
        }
    }

    public Task<IReadOnlyCollection<GroupInvite>> ListInvitesForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.GroupInvites.Values
                .Where(invite => invite.GroupId == groupId)
                .OrderByDescending(invite => invite.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<GroupInvite>>(items);
        }
    }

    public Task<IReadOnlyCollection<GroupInvite>> ListInvitesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var items = store.GroupInvites.Values
                .Where(invite => invite.InvitedUserId == userId)
                .OrderByDescending(invite => invite.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<GroupInvite>>(items);
        }
    }

    public Task SaveInviteAsync(GroupInvite invite, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            store.GroupInvites[invite.Id] = invite;
            return Task.CompletedTask;
        }
    }

    private static string ToMemberKey(Guid groupId, Guid userId) => $"{groupId:N}:{userId:N}";
}