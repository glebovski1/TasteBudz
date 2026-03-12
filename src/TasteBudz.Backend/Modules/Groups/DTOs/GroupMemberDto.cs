using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupMemberDto(
    Guid UserId,
    string Username,
    string DisplayName,
    GroupMemberState State,
    DateTimeOffset JoinedAtUtc);
