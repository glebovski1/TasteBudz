// Persistence boundary for groups, memberships, and group invites.
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// Stores group aggregates together with membership and invite records.
/// </summary>
public interface IGroupRepository
{
    Task<Group?> GetAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Group>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Group group, CancellationToken cancellationToken = default);

    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupMember>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupMember>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveMemberAsync(GroupMember member, CancellationToken cancellationToken = default);

    Task<GroupInvite?> GetInviteAsync(Guid inviteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupInvite>> ListInvitesForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupInvite>> ListInvitesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveInviteAsync(GroupInvite invite, CancellationToken cancellationToken = default);
}