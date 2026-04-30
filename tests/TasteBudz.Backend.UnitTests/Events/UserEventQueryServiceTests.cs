// Unit tests for user-scoped event dashboard query workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
using TasteBudz.Backend.Modules.Profiles;
using TasteBudz.Backend.UnitTests.Shared;

namespace TasteBudz.Backend.UnitTests.Events;

/// <summary>
/// Verifies that user event query reads honor lifecycle synchronization before filtering results.
/// </summary>
public sealed class UserEventQueryServiceTests
{
    [Fact]
    public async Task ListActiveForUserAsync_SynchronizesBeforeApplyingActiveFilter()
    {
        var now = new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero);
        var clock = new TestClock(now);
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, profileRepository, lifecycleService);
        var hostUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Stored as OPEN, but with DecisionAt in the past and too few participants this should cancel on read.
        await eventRepository.SaveAsync(new Event(
            eventId,
            hostUserId,
            "Needs cancellation",
            EventType.Open,
            EventStatus.Open,
            now.AddHours(2),
            now.AddMinutes(-5),
            4,
            2,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            null,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(
            eventId,
            hostUserId,
            EventParticipantState.Joined,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));

        var events = await queryService.ListActiveForUserAsync(hostUserId);
        var synchronized = await eventRepository.GetAsync(eventId);

        Assert.Empty(events);
        Assert.Equal(EventStatus.Cancelled, synchronized!.Status);
    }

    [Fact]
    public async Task ListForUserAsync_IncludesHostedJoinedInvitedAndGroupLinkedEventsAcrossStatuses()
    {
        var now = new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero);
        var clock = new TestClock(now);
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, profileRepository, lifecycleService);
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var hosted = CreateEvent(currentUserId, "Hosted completed", EventType.Open, EventStatus.Completed, now.AddDays(-2), null);
        var joined = CreateEvent(hostUserId, "Joined cancelled", EventType.Open, EventStatus.Cancelled, now.AddDays(-1), null);
        var invited = CreateEvent(hostUserId, "Invited full", EventType.Closed, EventStatus.Full, now.AddDays(1), null) with
        {
            Capacity = 2,
        };
        var groupLinked = CreateEvent(hostUserId, "Group linked open", EventType.Open, EventStatus.Open, now.AddDays(2), groupId);
        var unrelated = CreateEvent(hostUserId, "Unrelated open", EventType.Open, EventStatus.Open, now.AddDays(3), null);

        foreach (var eventRecord in new[] { hosted, joined, invited, groupLinked, unrelated })
        {
            await eventRepository.SaveAsync(eventRecord);
            await eventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, eventRecord.HostUserId, EventParticipantState.Joined, null, now.AddDays(-4), now.AddDays(-4), null, null));
        }

        await eventRepository.SaveParticipantAsync(new EventParticipant(joined.Id, currentUserId, EventParticipantState.Joined, null, now.AddDays(-3), now.AddDays(-3), null, null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(invited.Id, currentUserId, EventParticipantState.Invited, now.AddDays(-1), null, null, null, null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(invited.Id, Guid.NewGuid(), EventParticipantState.Joined, null, now.AddDays(-2), now.AddDays(-2), null, null));

        var events = await queryService.ListForUserAsync(currentUserId, new[] { groupId });

        Assert.Equal(
            new[] { "Hosted completed", "Joined cancelled", "Invited full", "Group linked open" },
            events.Select(item => item.Title));
        Assert.Contains(events, item => item.Title == "Hosted completed" && item.IsHosted && item.Status == EventStatus.Completed);
        Assert.Contains(events, item => item.Title == "Joined cancelled" && item.IsJoined && item.Status == EventStatus.Cancelled);
        Assert.Contains(events, item => item.Title == "Invited full" && item.IsInvited && item.Status == EventStatus.Full);
        Assert.Contains(events, item => item.Title == "Group linked open" && item.IsGroupLinked && item.GroupId == groupId);
        Assert.DoesNotContain(events, item => item.Title == "Unrelated open");
    }

    [Fact]
    public async Task ListForUserAsync_ExcludesLiveEventsWithBlockedParticipants()
    {
        var now = new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero);
        var clock = new TestClock(now);
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, profileRepository, lifecycleService);
        var currentUserId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var blockedUserId = Guid.NewGuid();

        var visibleEvent = CreateEvent(hostUserId, "Visible joined", EventType.Open, EventStatus.Open, now.AddDays(1), null);
        var blockedEvent = CreateEvent(hostUserId, "Blocked joined", EventType.Open, EventStatus.Open, now.AddDays(2), null);

        foreach (var eventRecord in new[] { visibleEvent, blockedEvent })
        {
            await eventRepository.SaveAsync(eventRecord);
            await eventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, hostUserId, EventParticipantState.Joined, null, now.AddDays(-4), now.AddDays(-4), null, null));
            await eventRepository.SaveParticipantAsync(new EventParticipant(eventRecord.Id, currentUserId, EventParticipantState.Joined, null, now.AddDays(-3), now.AddDays(-3), null, null));
        }

        await eventRepository.SaveParticipantAsync(new EventParticipant(blockedEvent.Id, blockedUserId, EventParticipantState.Joined, null, now.AddDays(-2), now.AddDays(-2), null, null));
        await profileRepository.SaveBlockAsync(new UserBlock(currentUserId, blockedUserId, now.AddHours(-1)));

        var events = await queryService.ListForUserAsync(currentUserId, Array.Empty<Guid>());

        Assert.Contains(events, item => item.EventId == visibleEvent.Id);
        Assert.DoesNotContain(events, item => item.EventId == blockedEvent.Id);
    }

    [Fact]
    public async Task ListPendingInvitesForUserAsync_ExcludesInvitesAfterDecisionAt()
    {
        var now = new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero);
        var clock = new TestClock(now);
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var profileRepository = new InMemoryProfileRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, profileRepository, lifecycleService);
        var hostUserId = Guid.NewGuid();
        var guestUserId = Guid.NewGuid();
        var staleInviteEventId = Guid.NewGuid();
        var pendingInviteEventId = Guid.NewGuid();

        await eventRepository.SaveAsync(new Event(
            staleInviteEventId,
            hostUserId,
            "Stale invite",
            EventType.Closed,
            EventStatus.Open,
            now.AddHours(6),
            now.AddMinutes(-10),
            4,
            2,
            null,
            "Sushi",
            null,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));
        await eventRepository.SaveAsync(new Event(
            pendingInviteEventId,
            hostUserId,
            "Actionable invite",
            EventType.Closed,
            EventStatus.Open,
            now.AddHours(6),
            now.AddHours(2),
            4,
            2,
            null,
            "Tacos",
            null,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));

        await eventRepository.SaveParticipantAsync(new EventParticipant(
            staleInviteEventId,
            hostUserId,
            EventParticipantState.Joined,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(
            staleInviteEventId,
            guestUserId,
            EventParticipantState.Invited,
            now.AddHours(-1),
            null,
            null,
            null,
            null));

        await eventRepository.SaveParticipantAsync(new EventParticipant(
            pendingInviteEventId,
            hostUserId,
            EventParticipantState.Joined,
            null,
            now.AddDays(-1),
            now.AddDays(-1),
            null,
            null));
        await eventRepository.SaveParticipantAsync(new EventParticipant(
            pendingInviteEventId,
            guestUserId,
            EventParticipantState.Invited,
            now.AddMinutes(-30),
            null,
            null,
            null,
            null));

        var invites = await queryService.ListPendingInvitesForUserAsync(guestUserId);
        var staleInviteEvent = await eventRepository.GetAsync(staleInviteEventId);

        var invite = Assert.Single(invites);
        Assert.Equal(pendingInviteEventId, invite.EventId);
        Assert.Equal(EventStatus.Cancelled, staleInviteEvent!.Status);
    }

    private static Event CreateEvent(
        Guid hostUserId,
        string title,
        EventType eventType,
        EventStatus status,
        DateTimeOffset startAtUtc,
        Guid? groupId) =>
        new(
            Guid.NewGuid(),
            hostUserId,
            title,
            eventType,
            status,
            startAtUtc,
            startAtUtc.AddHours(-1),
            4,
            2,
            null,
            "Sushi",
            groupId,
            status == EventStatus.Cancelled ? "Called off" : null,
            startAtUtc.AddDays(-5),
            startAtUtc.AddDays(-5),
            status == EventStatus.Cancelled ? startAtUtc.AddHours(-2) : null,
            status == EventStatus.Completed ? startAtUtc.AddHours(2) : null);
}
