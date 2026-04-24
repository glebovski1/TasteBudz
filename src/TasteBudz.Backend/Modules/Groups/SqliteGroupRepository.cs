using Microsoft.EntityFrameworkCore;
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Modules.Groups;

/// <summary>
/// SQLite-backed repository for groups, memberships, and invites.
/// </summary>
public sealed class SqliteGroupRepository(TasteBudzDbContext dbContext) : IGroupRepository
{
    public async Task<Group?> GetAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Groups.AsNoTracking().FirstOrDefaultAsync(group => group.Id == groupId, cancellationToken);
        return entity is null ? null : MapGroup(entity);
    }

    public async Task<IReadOnlyCollection<Group>> ListAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Groups.AsNoTracking().OrderBy(group => group.Name).ToListAsync(cancellationToken))
        .Select(MapGroup)
        .ToArray();

    public async Task SaveAsync(Group group, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Groups.FirstOrDefaultAsync(item => item.Id == group.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.Groups.Add(ToEntity(group));
        }
        else
        {
            entity.OwnerUserId = group.OwnerUserId;
            entity.Name = group.Name;
            entity.Description = group.Description;
            entity.Visibility = group.Visibility;
            entity.WallpaperTheme = group.WallpaperTheme;
            entity.LifecycleState = group.LifecycleState;
            entity.CreatedAtUtc = group.CreatedAtUtc;
            entity.UpdatedAtUtc = group.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GroupMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(member => member.GroupId == groupId && member.UserId == userId, cancellationToken);
        return entity is null ? null : MapMember(entity);
    }

    public async Task<IReadOnlyCollection<GroupMember>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        (await dbContext.GroupMembers
            .AsNoTracking()
            .Where(member => member.GroupId == groupId)
            .ToListAsync(cancellationToken))
        .Select(MapMember)
        .OrderBy(member => member.CreatedAtUtc)
        .ToArray();

    public async Task<IReadOnlyCollection<GroupMember>> ListMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.GroupMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .ToListAsync(cancellationToken))
        .Select(MapMember)
        .OrderByDescending(member => member.UpdatedAtUtc)
        .ToArray();

    public async Task SaveMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GroupMembers.FirstOrDefaultAsync(item => item.GroupId == member.GroupId && item.UserId == member.UserId, cancellationToken);

        if (entity is null)
        {
            dbContext.GroupMembers.Add(ToEntity(member));
        }
        else
        {
            entity.State = member.State;
            entity.CreatedAtUtc = member.CreatedAtUtc;
            entity.UpdatedAtUtc = member.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GroupInvite?> GetInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GroupInvites.AsNoTracking().FirstOrDefaultAsync(invite => invite.Id == inviteId, cancellationToken);
        return entity is null ? null : MapInvite(entity);
    }

    public async Task<IReadOnlyCollection<GroupInvite>> ListInvitesForGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        (await dbContext.GroupInvites
            .AsNoTracking()
            .Where(invite => invite.GroupId == groupId)
            .ToListAsync(cancellationToken))
        .Select(MapInvite)
        .OrderByDescending(invite => invite.CreatedAtUtc)
        .ToArray();

    public async Task<IReadOnlyCollection<GroupInvite>> ListInvitesForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await dbContext.GroupInvites
            .AsNoTracking()
            .Where(invite => invite.InvitedUserId == userId)
            .ToListAsync(cancellationToken))
        .Select(MapInvite)
        .OrderByDescending(invite => invite.CreatedAtUtc)
        .ToArray();

    public async Task SaveInviteAsync(GroupInvite invite, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GroupInvites.FirstOrDefaultAsync(item => item.Id == invite.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.GroupInvites.Add(ToEntity(invite));
        }
        else
        {
            entity.GroupId = invite.GroupId;
            entity.InvitedUserId = invite.InvitedUserId;
            entity.InviterUserId = invite.InviterUserId;
            entity.Status = invite.Status;
            entity.CreatedAtUtc = invite.CreatedAtUtc;
            entity.UpdatedAtUtc = invite.UpdatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GroupAnnouncement>> ListAnnouncementsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        (await dbContext.GroupAnnouncements
            .AsNoTracking()
            .Where(announcement => announcement.GroupId == groupId)
            .ToListAsync(cancellationToken))
        .Select(MapAnnouncement)
        .OrderByDescending(announcement => announcement.CreatedAtUtc)
        .ToArray();

    public async Task SaveAnnouncementAsync(GroupAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.GroupAnnouncements.FirstOrDefaultAsync(item => item.Id == announcement.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.GroupAnnouncements.Add(ToEntity(announcement));
        }
        else
        {
            entity.GroupId = announcement.GroupId;
            entity.AuthorUserId = announcement.AuthorUserId;
            entity.AnnouncementType = announcement.AnnouncementType;
            entity.Title = announcement.Title;
            entity.Body = announcement.Body;
            entity.RelatedEventId = announcement.RelatedEventId;
            entity.CreatedAtUtc = announcement.CreatedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Group MapGroup(GroupEntity entity) =>
        new(
            entity.Id,
            entity.OwnerUserId,
            entity.Name,
            entity.Description,
            entity.Visibility,
            entity.WallpaperTheme,
            entity.LifecycleState,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static GroupAnnouncement MapAnnouncement(GroupAnnouncementEntity entity) =>
        new(
            entity.Id,
            entity.GroupId,
            entity.AuthorUserId,
            entity.AnnouncementType,
            entity.Title,
            entity.Body,
            entity.RelatedEventId,
            entity.CreatedAtUtc);

    private static GroupMember MapMember(GroupMemberEntity entity) =>
        new(
            entity.GroupId,
            entity.UserId,
            entity.State,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static GroupInvite MapInvite(GroupInviteEntity entity) =>
        new(
            entity.Id,
            entity.GroupId,
            entity.InvitedUserId,
            entity.InviterUserId,
            entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static GroupEntity ToEntity(Group group) =>
        new()
        {
            Id = group.Id,
            OwnerUserId = group.OwnerUserId,
            Name = group.Name,
            Description = group.Description,
            Visibility = group.Visibility,
            WallpaperTheme = group.WallpaperTheme,
            LifecycleState = group.LifecycleState,
            CreatedAtUtc = group.CreatedAtUtc,
            UpdatedAtUtc = group.UpdatedAtUtc,
        };

    private static GroupAnnouncementEntity ToEntity(GroupAnnouncement announcement) =>
        new()
        {
            Id = announcement.Id,
            GroupId = announcement.GroupId,
            AuthorUserId = announcement.AuthorUserId,
            AnnouncementType = announcement.AnnouncementType,
            Title = announcement.Title,
            Body = announcement.Body,
            RelatedEventId = announcement.RelatedEventId,
            CreatedAtUtc = announcement.CreatedAtUtc,
        };

    private static GroupMemberEntity ToEntity(GroupMember member) =>
        new()
        {
            GroupId = member.GroupId,
            UserId = member.UserId,
            State = member.State,
            CreatedAtUtc = member.CreatedAtUtc,
            UpdatedAtUtc = member.UpdatedAtUtc,
        };

    private static GroupInviteEntity ToEntity(GroupInvite invite) =>
        new()
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            InvitedUserId = invite.InvitedUserId,
            InviterUserId = invite.InviterUserId,
            Status = invite.Status,
            CreatedAtUtc = invite.CreatedAtUtc,
            UpdatedAtUtc = invite.UpdatedAtUtc,
        };
}
