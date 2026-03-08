// Core event aggregate records.
namespace TasteBudz.Backend.Domain;

/// <summary>
/// Event aggregate root. Lifecycle state is server-owned and recalculated by backend workflows.
/// </summary>
public sealed record Event(
    Guid Id,
    Guid HostUserId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset DecisionAtUtc,
    int Capacity,
    int MinParticipantsToRun,
    Guid? SelectedRestaurantId,
    string? CuisineTarget,
    Guid? GroupId,
    string? CancellationReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? CompletedAtUtc);

/// <summary>
/// User-specific participation state within an event.
/// </summary>
public sealed record EventParticipant(
    Guid EventId,
    Guid UserId,
    EventParticipantState State,
    DateTimeOffset? InvitedAtUtc,
    DateTimeOffset? JoinedAtUtc,
    DateTimeOffset? RespondedAtUtc,
    DateTimeOffset? LeftAtUtc,
    DateTimeOffset? RemovedAtUtc);