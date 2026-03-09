// Unit tests for persistent-group workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Auth;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Infrastructure.ProblemDetails;
using TasteBudz.Backend.Modules.Auth;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Groups;
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
        var notificationService = new InMemoryNotificationService(store);
        var authService = new AuthService(authRepository, profileRepository, new Pbkdf2PasswordHasher(), new SecureTokenGenerator(), clock);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var groupService = new GroupService(groupRepository, eventRepository, authRepository, profileRepository, notificationService, lifecycleService, clock);

        return new TestServices(authService, groupService, notificationService);
    }

    private sealed record TestServices(
        AuthService AuthService,
        GroupService GroupService,
        INotificationService NotificationService);
}
