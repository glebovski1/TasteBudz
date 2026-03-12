using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Events;

public sealed record EventParticipantDto(
    Guid UserId,
    string Username,
    string DisplayName,
    EventParticipantState State,
    DateTimeOffset? InvitedAtUtc,
    DateTimeOffset? JoinedAtUtc,
    DateTimeOffset? RespondedAtUtc);
