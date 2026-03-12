using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record DashboardEventSummaryDto(
    Guid EventId,
    string? Title,
    EventStatus Status,
    DateTimeOffset EventStartAtUtc);
