// Recalculates event lifecycle state from the current clock and participant counts.
using TasteBudz.Backend.Domain;
using TasteBudz.Backend.Infrastructure.Time;
using TasteBudz.Backend.Modules.Notifications;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Keeps the server-owned event status aligned with the current time and participation state.
/// </summary>
public sealed class EventLifecycleService(
    IEventRepository eventRepository,
    INotificationService notificationService,
    IClock clock)
{
    public async Task<Event> SynchronizeAsync(Event eventRecord, CancellationToken cancellationToken = default)
    {
        var participants = await eventRepository.ListParticipantsAsync(eventRecord.Id, cancellationToken);
        var updated = Evaluate(eventRecord, participants, clock.UtcNow);

        if (updated != eventRecord)
        {
            await eventRepository.SaveAsync(updated, cancellationToken);
            await WriteLifecycleNotificationsAsync(eventRecord, updated, participants, cancellationToken);
        }

        return updated;
    }

    public Event Evaluate(Event eventRecord, IReadOnlyCollection<EventParticipant> participants, DateTimeOffset now)
    {
        if (eventRecord.Status == EventStatus.Cancelled || eventRecord.Status == EventStatus.Completed)
        {
            return eventRecord;
        }

        var activeParticipants = participants.Count(participant => participant.State == EventParticipantState.Joined);

        if (eventRecord.Status == EventStatus.Confirmed)
        {
            if (now >= eventRecord.EventStartAtUtc)
            {
                return eventRecord with
                {
                    Status = EventStatus.Completed,
                    CompletedAtUtc = now,
                    UpdatedAtUtc = now,
                };
            }

            // Once confirmed, the event stays confirmed until it completes.
            return eventRecord;
        }

        if (now >= eventRecord.DecisionAtUtc)
        {
            if (activeParticipants >= eventRecord.MinParticipantsToRun)
            {
                return eventRecord with
                {
                    Status = EventStatus.Confirmed,
                    UpdatedAtUtc = now,
                };
            }

            return eventRecord with
            {
                Status = EventStatus.Cancelled,
                CancellationReason = eventRecord.CancellationReason ?? "The event did not reach the minimum participant count by DecisionAt.",
                CancelledAtUtc = now,
                UpdatedAtUtc = now,
            };
        }

        var calculatedStatus = activeParticipants >= eventRecord.Capacity ? EventStatus.Full : EventStatus.Open;
        return eventRecord with
        {
            Status = calculatedStatus,
            UpdatedAtUtc = eventRecord.Status == calculatedStatus ? eventRecord.UpdatedAtUtc : now,
        };
    }

    private async Task WriteLifecycleNotificationsAsync(
        Event previous,
        Event current,
        IReadOnlyCollection<EventParticipant> participants,
        CancellationToken cancellationToken)
    {
        if (previous.Status == current.Status)
        {
            return;
        }

        if (current.Status is not EventStatus.Confirmed and not EventStatus.Cancelled)
        {
            return;
        }

        var recipientIds = participants
            .Where(participant => participant.State is EventParticipantState.Joined or EventParticipantState.Invited)
            .Select(participant => participant.UserId)
            .Append(current.HostUserId)
            .Distinct()
            .ToArray();

        var notificationType = current.Status == EventStatus.Confirmed
            ? NotificationType.EventConfirmed
            : NotificationType.EventCancelled;
        var message = current.Status == EventStatus.Confirmed
            ? "Your event is confirmed."
            : current.CancellationReason ?? "Your event was cancelled.";

        foreach (var recipientId in recipientIds)
        {
            await notificationService.CreateAsync(
                new Notification(Guid.NewGuid(), recipientId, notificationType, "Event", current.Id, message, clock.UtcNow, null),
                cancellationToken);
        }
    }
}