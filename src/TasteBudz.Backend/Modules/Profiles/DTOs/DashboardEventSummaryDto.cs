using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardEventSummaryDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc,
    string? CuisineTarget,
    Guid? GroupId = null,
    bool IsHosted = false,
    bool IsJoined = false,
    bool IsInvited = false,
    bool IsGroupLinked = false);
