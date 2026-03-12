using TasteBudz.Backend.Domain;

namespace TasteBudz.Backend.Modules.Groups;

public sealed record GroupDetailDto(
    Guid GroupId,
    Guid OwnerUserId,
    string Name,
    string? Description,
    GroupVisibility Visibility,
    GroupLifecycleState LifecycleState,
    bool IsCurrentUserMember,
    IReadOnlyCollection<GroupMemberDto> Members);
