using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Profiles;

public sealed record EventInviteDto(
    Guid EventId,
    string? Title,
    EventType EventType,
    DateTimeOffset EventStartAtUtc,
    DateTimeOffset? InvitedAtUtc);
