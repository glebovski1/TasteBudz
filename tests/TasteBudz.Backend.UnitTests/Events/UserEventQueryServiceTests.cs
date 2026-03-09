// Unit tests for user-scoped event dashboard query workflows.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Persistence.InMemory;
using TasteBudz.Backend.Modules.Events;
using TasteBudz.Backend.Modules.Notifications;
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
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, lifecycleService);
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
    public async Task ListPendingInvitesForUserAsync_ExcludesInvitesAfterDecisionAt()
    {
        var now = new DateTimeOffset(2026, 3, 9, 18, 0, 0, TimeSpan.Zero);
        var clock = new TestClock(now);
        var store = new InMemoryTasteBudzStore();
        store.Reset();
        var eventRepository = new InMemoryEventRepository(store);
        var notificationService = new InMemoryNotificationService(store);
        var lifecycleService = new EventLifecycleService(eventRepository, notificationService, clock);
        var queryService = new UserEventQueryService(eventRepository, lifecycleService);
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
}
