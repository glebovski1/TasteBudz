// Request and response contracts for event browse, creation, updates, participation, and invites.
using System.ComponentModel.DataAnnotations;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

/// <summary>
/// Event card returned by browse endpoints.
/// </summary>
public sealed record EventSummaryDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset DecisionAtUtc,
    int Capacity,
    int ActiveParticipants,
    Guid HostUserId,
    Guid? SelectedRestaurantId,
    string? CuisineTarget,
    Guid? GroupId);

/// <summary>
/// Full event payload returned by detail endpoints.
/// </summary>
public sealed record EventDetailDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset DecisionAtUtc,
    int Capacity,
    int MinParticipantsToRun,
    int ActiveParticipants,
    Guid HostUserId,
    Guid? SelectedRestaurantId,
    string? CuisineTarget,
    Guid? GroupId,
    string? CancellationReason);

/// <summary>
/// Participant view returned by event membership endpoints.
/// </summary>
public sealed record EventParticipantDto(
    Guid UserId,
    string Username,
    string DisplayName,
    EventParticipantState State,
    DateTimeOffset? InvitedAtUtc,
    DateTimeOffset? JoinedAtUtc,
    DateTimeOffset? RespondedAtUtc);

/// <summary>
/// Request payload for creating an event.
/// </summary>
public sealed class CreateEventRequest
{
    [Required]
    public EventType? EventType { get; init; }

    [Required]
    public DateTimeOffset? EventStartAtUtc { get; init; }

    [Required]
    [Range(2, 8)]
    public int? Capacity { get; init; }

    [MaxLength(120)]
    public string? Title { get; init; }

    public Guid? SelectedRestaurantId { get; init; }

    [MaxLength(120)]
    public string? CuisineTarget { get; init; }

    public Guid? GroupId { get; init; }

    public IReadOnlyCollection<string> InviteUsernames { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Partial update payload for host-managed event fields.
/// </summary>
public sealed class UpdateEventRequest
{
    [MaxLength(120)]
    public string? Title { get; init; }

    public DateTimeOffset? EventStartAtUtc { get; init; }

    [Range(2, 8)]
    public int? Capacity { get; init; }

    public Guid? SelectedRestaurantId { get; init; }

    [MaxLength(120)]
    public string? CuisineTarget { get; init; }

    public Guid? GroupId { get; init; }
}

/// <summary>
/// Request payload for the current user's event participation changes.
/// </summary>
public sealed class UpdateMyParticipationRequest
{
    [Required]
    public EventParticipantState? State { get; init; }
}

/// <summary>
/// Request payload for inviting users to a closed event.
/// </summary>
public sealed class InviteUsersRequest
{
    [Required]
    public IReadOnlyCollection<string> Usernames { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Request payload for event cancellation.
/// </summary>
public sealed class CancelEventRequest
{
    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Query parameters for browsing open events.
/// </summary>
public sealed class BrowseEventsQuery
{
    public string? Q { get; init; }

    public string? Cuisine { get; init; }

    public PriceTier? PriceTier { get; init; }

    public EventStatus? Status { get; init; }

    public EventType? EventType { get; init; }

    [RegularExpression("^[0-9]{5}$")]
    public string? ZipCode { get; init; }

    [Range(1, 100)]
    public double? RadiusMiles { get; init; }

    public DateTimeOffset? StartsAfter { get; init; }

    public DateTimeOffset? StartsBefore { get; init; }

    public bool AvailabilityOnly { get; init; }

    public Guid? GroupId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}