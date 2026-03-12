using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupInviteDto(
    Guid InviteId,
    Guid GroupId,
    Guid InvitedUserId,
    string InvitedUsername,
    GroupInviteStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
