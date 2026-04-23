using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupMemberDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Bio,
    SocialGoal? SocialGoal,
    string? HomeAreaZipCode,
    Guid? AvatarMediaAssetId,
    IReadOnlyCollection<string> CuisineTags,
    IReadOnlyCollection<string> DietaryFlags,
    GroupMemberState State,
    DateTimeOffset JoinedAtUtc);
