// Unit tests for persistent-group workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
using TasteBudz.Backend.Modules.Media;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Groups;

/// <summary>
/// Verifies the main owner/member group rules in isolation.
/// </summary>
public sealed class GroupServiceTests
{
    [Fact]
    public async Task CreateAsync_AutoCreatesOwnerMembership()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");

        var detail = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Weekend Crew",
            Visibility = GroupVisibility.Private,
        });

        Assert.True(detail.IsCurrentUserMember);
        Assert.Contains(detail.Members, member => member.UserId == owner.CurrentUser.UserId && member.State == GroupMemberState.Active);
    }

    [Fact]
    public async Task JoinAsync_PublicGroupActivatesMembership()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var detail = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Public Crew",
            Visibility = GroupVisibility.Public,
        });

        var joined = await services.GroupService.JoinAsync(guest.CurrentUser.UserId, detail.GroupId);

        Assert.True(joined.IsCurrentUserMember);
        Assert.Contains(joined.Members, member => member.UserId == guest.CurrentUser.UserId && member.State == GroupMemberState.Active);
    }

    [Fact]
    public async Task BrowseAsync_ExcludesPublicGroupsWhereCurrentUserIsAlreadyActiveMember()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var joinedGroup = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Joined Crew",
            Visibility = GroupVisibility.Public,
        });
        var openGroup = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Open Crew",
            Visibility = GroupVisibility.Public,
        });

        await services.GroupService.JoinAsync(guest.CurrentUser.UserId, joinedGroup.GroupId);

        var browse = await services.GroupService.BrowseAsync(guest.CurrentUser.UserId, new BrowseGroupsQuery
        {
            PageSize = 10,
        });

        Assert.DoesNotContain(browse.Items, item => item.GroupId == joinedGroup.GroupId);
        Assert.Contains(browse.Items, item => item.GroupId == openGroup.GroupId);
    }

    [Fact]
    public async Task BrowseAsync_ExcludesPublicGroupsWithBlockedOwnerOrActiveMember()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var visibleOwner = await RegisterAsync(services.AuthService, "visibleowner", "visibleowner@example.com");
        var blockedOwner = await RegisterAsync(services.AuthService, "blockedowner", "blockedowner@example.com");
        var memberOwner = await RegisterAsync(services.AuthService, "memberowner", "memberowner@example.com");
        var blockedMember = await RegisterAsync(services.AuthService, "blockedmember", "blockedmember@example.com");

        var visibleGroup = await services.GroupService.CreateAsync(ToCurrentUser(visibleOwner), new CreateGroupRequest
        {
            Name = "Visible Crew",
            Visibility = GroupVisibility.Public,
        });
        await services.GroupService.CreateAsync(ToCurrentUser(blockedOwner), new CreateGroupRequest
        {
            Name = "Blocked Owner Crew",
            Visibility = GroupVisibility.Public,
        });
        var blockedMemberGroup = await services.GroupService.CreateAsync(ToCurrentUser(memberOwner), new CreateGroupRequest
        {
            Name = "Blocked Member Crew",
            Visibility = GroupVisibility.Public,
        });
        await services.GroupService.JoinAsync(blockedMember.CurrentUser.UserId, blockedMemberGroup.GroupId);
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(guest.CurrentUser.UserId, blockedOwner.CurrentUser.UserId, clock.UtcNow));
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(blockedMember.CurrentUser.UserId, guest.CurrentUser.UserId, clock.UtcNow));

        var browse = await services.GroupService.BrowseAsync(guest.CurrentUser.UserId, new BrowseGroupsQuery
        {
            PageSize = 10,
        });

        var item = Assert.Single(browse.Items);
        Assert.Equal(visibleGroup.GroupId, item.GroupId);
    }

    [Fact]
    public async Task GetAsync_WhenPublicGroupHasBlockedActiveMember_ReturnsNotFound()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var blockedMember = await RegisterAsync(services.AuthService, "blockedmember", "blockedmember@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Public Crew",
            Visibility = GroupVisibility.Public,
        });
        await services.GroupService.JoinAsync(blockedMember.CurrentUser.UserId, group.GroupId);
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(blockedMember.CurrentUser.UserId, guest.CurrentUser.UserId, clock.UtcNow));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.GetAsync(guest.CurrentUser.UserId, group.GroupId));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task JoinAsync_WhenPublicGroupHasBlockedActiveMember_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var blockedMember = await RegisterAsync(services.AuthService, "blockedmember", "blockedmember@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Public Crew",
            Visibility = GroupVisibility.Public,
        });
        await services.GroupService.JoinAsync(blockedMember.CurrentUser.UserId, group.GroupId);
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(guest.CurrentUser.UserId, blockedMember.CurrentUser.UserId, clock.UtcNow));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.JoinAsync(guest.CurrentUser.UserId, group.GroupId));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task RespondToInviteAsync_AcceptCreatesPrivateMembership()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Private Crew",
            Visibility = GroupVisibility.Private,
        });

        var invite = await services.GroupService.InviteAsync(ToCurrentUser(owner), group.GroupId, new InviteUserToGroupRequest
        {
            Username = guest.CurrentUser.Username,
        });

        var updated = await services.GroupService.RespondToInviteAsync(ToCurrentUser(guest), invite.InviteId, new RespondToGroupInviteRequest
        {
            Status = GroupInviteStatus.Accepted,
        });
        var detail = await services.GroupService.GetAsync(guest.CurrentUser.UserId, group.GroupId);

        Assert.Equal(GroupInviteStatus.Accepted, updated.Status);
        Assert.Contains(detail.Members, member => member.UserId == guest.CurrentUser.UserId && member.State == GroupMemberState.Active);
        Assert.Single(await services.NotificationService.ListForUserAsync(guest.CurrentUser.UserId));
    }

    [Fact]
    public async Task InviteAsync_CreatesNotificationTargetingInviteResponse()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Private Crew",
            Visibility = GroupVisibility.Private,
        });

        var invite = await services.GroupService.InviteAsync(ToCurrentUser(owner), group.GroupId, new InviteUserToGroupRequest
        {
            Username = guest.CurrentUser.Username,
        });

        var notification = Assert.Single(await services.NotificationService.ListForUserAsync(guest.CurrentUser.UserId));
        Assert.Equal(NotificationType.GroupInviteReceived, notification.NotificationType);
        Assert.Equal("GroupInvite", notification.ContextType);
        Assert.Equal(invite.InviteId, notification.ContextId);
    }

    [Fact]
    public async Task InviteAsync_RemovedMemberIsRejected()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Removed private crew",
            Visibility = GroupVisibility.Public,
        });

        await services.GroupService.JoinAsync(guest.CurrentUser.UserId, group.GroupId);
        await services.GroupService.RemoveMemberAsync(ToCurrentUser(owner), group.GroupId, guest.CurrentUser.UserId);
        await services.GroupService.UpdateAsync(ToCurrentUser(owner), group.GroupId, new UpdateGroupRequest
        {
            Visibility = GroupVisibility.Private,
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.InviteAsync(ToCurrentUser(owner), group.GroupId, new InviteUserToGroupRequest
            {
                Username = guest.CurrentUser.Username,
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task RespondToInviteAsync_WhenInviteeBlocksActiveMember_ReturnsForbidden()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var activeMember = await RegisterAsync(services.AuthService, "member", "member@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Blocked private crew",
            Visibility = GroupVisibility.Public,
        });

        await services.GroupService.JoinAsync(activeMember.CurrentUser.UserId, group.GroupId);
        await services.GroupService.UpdateAsync(ToCurrentUser(owner), group.GroupId, new UpdateGroupRequest
        {
            Visibility = GroupVisibility.Private,
        });
        var invite = await services.GroupService.InviteAsync(ToCurrentUser(owner), group.GroupId, new InviteUserToGroupRequest
        {
            Username = guest.CurrentUser.Username,
        });
        await services.ProfileRepository.SaveBlockAsync(new UserBlock(activeMember.CurrentUser.UserId, guest.CurrentUser.UserId, clock.UtcNow));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.RespondToInviteAsync(ToCurrentUser(guest), invite.InviteId, new RespondToGroupInviteRequest
            {
                Status = GroupInviteStatus.Accepted,
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task LeaveAsync_OwnerIsRejected()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Owners stay",
            Visibility = GroupVisibility.Public,
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.LeaveAsync(owner.CurrentUser.UserId, group.GroupId));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ExcludesInactiveMembersFromDetail()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Public Crew",
            Visibility = GroupVisibility.Public,
        });

        await services.GroupService.JoinAsync(guest.CurrentUser.UserId, group.GroupId);
        await services.GroupService.RemoveMemberAsync(ToCurrentUser(owner), group.GroupId, guest.CurrentUser.UserId);

        var detail = await services.GroupService.GetAsync(owner.CurrentUser.UserId, group.GroupId);

        Assert.DoesNotContain(detail.Members, member => member.UserId == guest.CurrentUser.UserId);
    }

    [Fact]
    public async Task UpdateAsync_OwnerCanSetWallpaperTheme()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Wallpaper crew",
            Visibility = GroupVisibility.Public,
        });

        var updated = await services.GroupService.UpdateAsync(ToCurrentUser(owner), group.GroupId, new UpdateGroupRequest
        {
            WallpaperTheme = GroupWallpaperTheme.SushiBar,
        });

        Assert.Equal(GroupWallpaperTheme.SushiBar, updated.WallpaperTheme);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_NonOwnerIsRejected()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var guest = await RegisterAsync(services.AuthService, "guest", "guest@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Owner posts",
            Visibility = GroupVisibility.Public,
        });

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            services.GroupService.CreateAnnouncementAsync(ToCurrentUser(guest), group.GroupId, new CreateGroupAnnouncementRequest
            {
                Title = "Menu update",
                Body = "Trying noodles this week.",
            }));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAnnouncementAsync_OwnerPostAppearsInList()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        var services = CreateServices(clock);
        var owner = await RegisterAsync(services.AuthService, "owner", "owner@example.com");
        var group = await services.GroupService.CreateAsync(ToCurrentUser(owner), new CreateGroupRequest
        {
            Name = "Owner posts",
            Visibility = GroupVisibility.Public,
        });

        await services.GroupService.CreateAnnouncementAsync(ToCurrentUser(owner), group.GroupId, new CreateGroupAnnouncementRequest
        {
            Title = "Menu update",
            Body = "Trying noodles this week.",
        });

        var announcements = await services.GroupService.ListAnnouncementsAsync(owner.CurrentUser.UserId, group.GroupId);

        var announcement = Assert.Single(announcements.Items);
        Assert.Equal(GroupAnnouncementType.OwnerPost, announcement.AnnouncementType);
        Assert.Equal("Menu update", announcement.Title);
    }

    private static async Task<SessionDto> RegisterAsync(AuthService authService, string username, string email) =>
        await authService.RegisterAsync(new RegisterUserRequest
        {
            Username = username,
            Email = email,
            Password = "Pa$$w0rd123",
            ZipCode = "45220",
        });

    private static CurrentUser ToCurrentUser(SessionDto session) =>
        new(session.CurrentUser.UserId, session.CurrentUser.Username, session.CurrentUser.Roles);

    private static TestServices CreateServices(TestClock clock)
    {
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var authRepository = new InMemoryAuthRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var eventRepository = new InMemoryEventRepository(store);
        var groupRepository = new InMemoryGroupRepository(store);
        var mediaRepository = new InMemoryMediaRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var groupService = new GroupService(groupRepository, eventRepository, authRepository, profileRepository, mediaRepository, notificationService, lifecycleService, clock);

        return new TestServices(authService, groupService, profileRepository, notificationService);
    }

    private sealed record TestServices(
        AuthService AuthService,
        GroupService GroupService,
        IProfileRepository ProfileRepository,
        INotificationService NotificationService);
}
